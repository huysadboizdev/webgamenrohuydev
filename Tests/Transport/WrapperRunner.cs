using System;
using System.Collections.Generic;
using System.Reflection;
using NroWeb;

public class TestWire : IWireTransport
{
    public Action Opened, Ended; public Action<byte[]> Bytes;
    public readonly List<byte[]> Sent = new List<byte[]>();
    public void Open(string host, int port, Action opened, Action<byte[]> bytes, Action ended) { Opened = opened; Bytes = bytes; Ended = ended; }
    public void Send(byte[] bytes) { Sent.Add(bytes); }
    public void Close() { }
}
public class TestHandler : Game1.IMessageHandler, Game2.IMessageHandler
{
    public int Opened, Failed, Disconnected;
    public readonly List<object> Messages = new List<object>();
    public void onMessage(Game1.Message m) { Messages.Add(m); }
    public void onMessage(Game2.Message m) { Messages.Add(m); }
    public void onConnectionFail(bool main) { Failed++; }
    public void onDisconnected(bool main) { Disconnected++; }
    public void onConnectOK(bool main) { Opened++; }
}
public class WrapperRunner
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
    static void Invoke(object session, string name, params object[] args) { session.GetType().GetMethod(name).Invoke(session, args); }
    static object Field(Type type, string field) { return type.GetField(field).GetValue(null); }
    static byte[] Server(int command, byte[] data, bool extended, ref int cursor)
    {
        var output = new List<byte>(); output.Add(unchecked((byte)command));
        if (extended) { output.Add(unchecked((byte)((data.Length & 255) - 128))); output.Add(unchecked((byte)(((data.Length >> 8) & 255) - 128))); output.Add(unchecked((byte)(((data.Length >> 16) & 255) - 128))); }
        else { output.Add((byte)(data.Length >> 8)); output.Add((byte)data.Length); }
        output.AddRange(data); byte[] key = { 7, 9, 13 };
        for (int i = 0; i < output.Count; i++) { output[i] ^= key[cursor]; cursor = (cursor + 1) % key.Length; }
        return output.ToArray();
    }
    public static void Main()
    {
        WebProtocolCases.Run(); WebSessionCases.Run(); WebTransportRegression.Run();
        foreach (string game in new[] { "Game1", "Game2" }) foreach (string name in new[] { "Session_ME", "Session_ME2" })
        {
            Type type = Type.GetType(game + "." + name); var wire = new TestWire(); var handler = new TestHandler();
            object session = Activator.CreateInstance(type, new object[] { wire });
            Invoke(session, "setHandler", handler); Invoke(session, "connect", "localhost", 14445);
            Check((bool)Field(type,"connecting"), "wrapper connecting " + type);
            wire.Opened(); Check(handler.Opened == 1 && (bool)Field(type,"connected"), "wrapper connected " + type);
            // Key plus secondary host/port/enable, followed by encrypted negotiation and inventory in same callback.
            var payload = new byte[] { 3, 7, 14, 4, 0, 1, 120, 0, 0, 0x38, 0x6d, 1 };
            var combined = new List<byte> { 229, 0, (byte)payload.Length }; combined.AddRange(payload);
            int cursor = 0; combined.AddRange(Server(-29, new byte[] { 100, 1, 1 }, false, ref cursor));
            combined.AddRange(Server(-30, new byte[] { 42 }, true, ref cursor)); wire.Bytes(combined.ToArray());
            Check((bool)Field(type,"UseInventory200") && (bool)Field(type,"UseItemOptionsInt32"), "wrapper negotiated flags " + type);
            Type controller = Type.GetType(game + ".Controller"); controller.GetField("isStopReadMessage").SetValue(null,true);
            type.GetMethod("update").Invoke(null,null); Check(handler.Messages.Count == 0, "paused dispatch " + type);
            controller.GetField("isStopReadMessage").SetValue(null,false); type.GetMethod("update").Invoke(null,null);
            Check(handler.Messages.Count == 2, "wrapper dispatch " + type);
            foreach (object message in handler.Messages) Check((bool)message.GetType().GetField("PacketInventory200").GetValue(message), "packet snapshot " + type);
            Type midlet = Type.GetType(game + ".GameMidlet"); Check((string)midlet.GetField("IP2").GetValue(null) == "x" && (int)midlet.GetField("PORT2").GetValue(null) == 14445, "secondary routing metadata " + type);
            Type canvas = Type.GetType(game + ".GameCanvas"); Check((int)canvas.GetField("Connected2").GetValue(null) == 1, "only primary launches secondary " + type);
            Invoke(session,"close"); Check(!(bool)Field(type,"connected") && !(bool)Field(type,"UseInventory200") && Field(type,"key") == null, "wrapper reset " + type);
            // A final valid frame immediately before EOF must still reach the controller.
            var eofWire = new TestWire(); var eofHandler = new TestHandler(); object eof = Activator.CreateInstance(type,new object[]{eofWire});
            Invoke(eof,"setHandler",eofHandler); Invoke(eof,"connect","localhost",14445); eofWire.Opened();
            eofWire.Bytes(new byte[]{229,0,4,3,7,14,4}); cursor=0; eofWire.Bytes(Server(5,new byte[]{99},false,ref cursor));
            eofWire.Ended(); Check(eofHandler.Messages.Count == 0 && eofHandler.Failed == 0,"EOF notification waits for decoded frame " + type);
            type.GetField("timeConnected",BindingFlags.NonPublic | BindingFlags.Instance).SetValue(eof, unchecked(Environment.TickCount - 1000));
            type.GetMethod("update").Invoke(null,null);
            Check(eofHandler.Messages.Count == 1 && eofHandler.Failed == 1,"EOF retains decoded frame and then reports termination " + type);
            // A paused main loop must not retain an unbounded stream of valid encrypted frames.
            var pausedWire = new TestWire(); var pausedHandler = new TestHandler();
            object paused = Activator.CreateInstance(type, new object[] { pausedWire }); Invoke(paused,"setHandler",pausedHandler);
            Invoke(paused,"connect","localhost",14445); pausedWire.Opened(); pausedWire.Bytes(new byte[] {229,0,4,3,7,14,4});
            controller.GetField("isStopReadMessage").SetValue(null,true); cursor = 0;
            var flood = new List<byte>(); for (int i = 0; i < 4097; i++) flood.AddRange(Server(5,new byte[0],false,ref cursor));
            pausedWire.Bytes(flood.ToArray());
            Check(!(bool)Field(type,"connected") && pausedHandler.Messages.Count == 0,"paused receive overflow terminates connection " + type);
            controller.GetField("isStopReadMessage").SetValue(null,false);
            type.GetMethod("update").Invoke(null,null);
            Check(pausedHandler.Messages.Count == 4096 && pausedHandler.Failed == 1,"bounded decoded queue drains before overflow notification " + type);
            if (game == "Game1" && name == "Session_ME")
            {
                var largeWire = new TestWire(); var largeHandler = new TestHandler(); object largeSession = Activator.CreateInstance(type,new object[]{largeWire});
                Invoke(largeSession,"setHandler",largeHandler); Invoke(largeSession,"connect","localhost",14445); largeWire.Opened();
                largeWire.Bytes(new byte[]{229,0,4,3,7,14,4}); cursor=0;
                var largeFlood = new List<byte>(); for (int i=0;i<129;i++) largeFlood.AddRange(Server(5,new byte[65535],false,ref cursor));
                largeWire.Bytes(largeFlood.ToArray());
                Check(!(bool)Field(type,"connected"),"receive byte overflow terminates connection");
                type.GetMethod("update").Invoke(null,null);
                Check(largeHandler.Messages.Count == 127 && largeHandler.Failed + largeHandler.Disconnected == 1,"receive byte limit drains bounded complete packets before notification");
            }
            Console.WriteLine("PASS: " + type + " actual browser wrapper, handshake routing, receive pause, packet snapshots and close");
        }
    }
}
