using System;
using System.Collections.Generic;
using NroWeb;
public static class WebSessionCases
{
    sealed class Wire : IWireTransport
    {
        public Action Opened; public Action<byte[]> Bytes; public Action Ended;
        public readonly List<byte[]> Sent = new List<byte[]>(); public int Closed;
        public void Open(string host, int port, Action opened, Action<byte[]> bytes, Action ended) { Opened = opened; Bytes = bytes; Ended = ended; }
        public void Send(byte[] bytes) { Sent.Add(bytes); }
        public void Close() { Closed++; }
    }
    static void Check(bool value, string label) { if (!value) throw new Exception(label); }
    public static void Run()
    {
        var wire = new Wire(); var core = new WebSessionCore(true, wire); int opened = 0, ended = 0, handshakes = 0, packets = 0;
        core.Opened = delegate { opened++; }; core.Ended = delegate(bool wasConnected) { ended++; };
        core.Handshake = delegate(ProtocolPacket packet) { handshakes++; }; core.Packet = delegate(ProtocolPacket packet) { packets++; };
        core.Connect("localhost", 14445);
        Check(core.Connecting && !core.Connected, "connect enters connecting state");
        var staleOpen = wire.Opened; var staleBytes = wire.Bytes; var staleEnd = wire.Ended;
        core.Send(7, new sbyte[] { 2 }); wire.Opened();
        Check(opened == 1 && core.Connected && !core.Connecting && wire.Sent.Count == 1 && wire.Sent[0][0] == 229, "open sends -27 before queued traffic");
        // One callback carries plaintext key handshake followed by encrypted data.
        wire.Bytes(new byte[] { 229, 0, 4, 3, 7, 14, 4, (byte)(4 ^ 7), 9, (byte)(1 ^ 13), (byte)(99 ^ 7) });
        Check(handshakes == 1 && packets == 1 && core.Codec.HasKey && wire.Sent.Count == 2, "handshake installs cumulative key before next combined frame and flushes queue");
        core.Close(); Check(!core.Connected && !core.Codec.HasKey && wire.Closed == 1, "intentional close resets protocol");
        core.Connect("localhost", 14445); staleOpen(); staleBytes(new byte[] { 229, 0, 0 }); staleEnd();
        Check(core.Connecting && opened == 1 && ended == 0, "late callbacks from old connection ignored");
        wire.Ended(); wire.Ended(); Check(ended == 1 && !core.Connecting, "failure reports exactly once");
        core.Connect("localhost", 14445); wire.Opened(); wire.Bytes(new byte[] { 229, 0, 1, 0 });
        Check(ended == 2 && !core.Connected, "malformed key safely terminates connection");
        var queuedWire = new Wire(); var queued = new WebSessionCore(true, queuedWire); int queueFailures = 0;
        queued.Ended = delegate(bool wasConnected) { queueFailures++; };
        queued.Connect("localhost", 14445);
        for (int i = 0; i < 4097; i++) queued.Send(1, null);
        Check(queueFailures == 1 && !queued.Connecting, "outbound queue count overflow terminates connection");
        var byteWire = new Wire(); var byteQueue = new WebSessionCore(true, byteWire); int byteFailures = 0;
        byteQueue.Ended = delegate(bool wasConnected) { byteFailures++; };
        byteQueue.Connect("localhost", 14445);
        for (int i = 0; i < 129; i++) byteQueue.Send(1, new sbyte[65535]);
        Check(byteFailures == 1 && !byteQueue.Connecting, "outbound queue byte overflow terminates connection");
        Console.WriteLine("PASS: session connect, handshake ordering, cumulative XOR key, queued messages, stale callbacks, close/reconnect, failure deduplication, malformed handshake");
    }
}
