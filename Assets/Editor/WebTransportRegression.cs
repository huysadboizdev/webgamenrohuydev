using System;
using System.Collections.Generic;
using NroWeb;

public static class WebTransportRegression
{
    sealed class FakeWire : IWireTransport
    {
        public Action opened; public Action<byte[]> bytes; public Action ended;
        public List<byte[]> sent = new List<byte[]>();
        public bool closed;
        public void Open(string h, int p, Action o, Action<byte[]> b, Action e) { opened = o; bytes = b; ended = e; }
        public void Send(byte[] data) { sent.Add((byte[])data.Clone()); }
        public void Close() { closed = true; }
        public void FireOpen() { if (opened != null) opened(); }
        public void FireData(byte[] data) { if (bytes != null) bytes(data); }
        public void FireEnded() { if (ended != null) ended(); }
    }

    sealed class ServerCodec
    {
        readonly ProtocolCodec encoder = new ProtocolCodec(true);
        public byte[] EncodeHandshake(byte[] rawKey)
        {
            var payload = new sbyte[rawKey.Length + 1];
            payload[0] = (sbyte)rawKey.Length;
            for (int i = 0; i < rawKey.Length; i++) payload[i + 1] = unchecked((sbyte)rawKey[i]);
            var bytes = new byte[3 + payload.Length];
            bytes[0] = unchecked((byte)-27);
            bytes[1] = (byte)(payload.Length >> 8);
            bytes[2] = (byte)payload.Length;
            Buffer.BlockCopy(payload, 0, bytes, 3, payload.Length);
            return bytes;
        }
        public byte[] EncodeData(sbyte command, sbyte[] payload) { return encoder.Encode(command, payload); }
        public void InstallKey(byte[] rawKey)
        {
            var key = (byte[])rawKey.Clone();
            for (int i = 1; i < key.Length; i++) key[i] ^= key[i - 1];
            encoder.InstallKey(key);
        }
    }

    static void Require(bool cond, string msg) { if (!cond) throw new Exception("FAIL: " + msg); }

    public static void Run()
    {
        TestHandshakeThenPacketThenDisconnect();
        TestFragmentedPacketOnDisconnect();
        TestQueuedPacketDiscardedAfterDisconnect();
        UnityEngine.Debug.Log("WebTransportRegression PASS: drain pending packets on disconnect");
    }

    static void TestHandshakeThenPacketThenDisconnect()
    {
        var wire = new FakeWire();
        var core = new WebSessionCore(true, wire);
        var packets = new List<ProtocolPacket>();
        var ended = false;
        core.Packet = p => packets.Add(p);
        core.Handshake = p => { };
        core.Opened = () => { };
        core.Ended = was => ended = true;

        core.Connect("dummy", 1);
        wire.FireOpen();

        var server = new ServerCodec();
        byte[] key = { 0x42 };
        wire.FireData(server.EncodeHandshake(key));
        server.InstallKey(key);
        wire.FireData(server.EncodeData(10, new sbyte[] { 1, 2, 3 }));
        wire.FireEnded();

        Require(ended, "Should report disconnect");
        Require(packets.Count == 1, "Complete packet received before disconnect should be delivered; got " + packets.Count);
    }

    static void TestFragmentedPacketOnDisconnect()
    {
        var wire = new FakeWire();
        var core = new WebSessionCore(true, wire);
        var packets = new List<ProtocolPacket>();
        var ended = false;
        core.Packet = p => packets.Add(p);
        core.Handshake = p => { };
        core.Ended = was => ended = true;

        core.Connect("dummy", 1);
        wire.FireOpen();

        var server = new ServerCodec();
        byte[] key = { 0x99 };
        wire.FireData(server.EncodeHandshake(key));
        server.InstallKey(key);

        byte[] data = server.EncodeData(10, new sbyte[] { 100, 101, 102, 103 });
        byte[] first = new byte[data.Length - 2];
        byte[] second = new byte[2];
        Array.Copy(data, 0, first, 0, first.Length);
        Array.Copy(data, first.Length, second, 0, second.Length);

        wire.FireData(first);
        wire.FireEnded();
        wire.FireData(second);

        Require(ended, "Should report disconnect before second fragment");
        Require(packets.Count == 0, "Incomplete packet should not be delivered; got " + packets.Count);
    }

    static void TestQueuedPacketDiscardedAfterDisconnect()
    {
        var wire = new FakeWire();
        var core = new WebSessionCore(true, wire);
        core.Opened = () => { };

        core.Connect("dummy", 1);
        wire.FireOpen();
        wire.sent.Clear();

        var server = new ServerCodec();
        byte[] key = { 0x55 };
        wire.FireData(server.EncodeHandshake(key));
        server.InstallKey(key);

        core.Send(5, new sbyte[] { 9, 9, 9 });
        Require(wire.sent.Count == 1, "First packet should be sent immediately after key");
        wire.sent.Clear();

        wire.FireEnded();

        core.Send(7, new sbyte[] { 1, 2 });
        Require(wire.sent.Count == 0, "Packet after disconnect should not be sent on wire");
        Require(wire.closed, "Wire should be closed after Fail");
        core.Close();
        Require(wire.closed, "Wire should be closed after Close");
    }
}
