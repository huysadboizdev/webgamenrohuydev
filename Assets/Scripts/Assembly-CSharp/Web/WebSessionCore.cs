using System;
using System.Collections.Generic;
namespace NroWeb
{
    public interface IWireTransport
    {
        void Open(string host, int port, Action opened, Action<byte[]> bytes, Action ended);
        void Send(byte[] bytes);
        void Close();
    }
    public sealed class WebSessionCore
    {
        public readonly ProtocolCodec Codec;
        readonly IWireTransport wire;
        sealed class Outbound { public sbyte Command; public sbyte[] Data; }
        readonly Queue<Outbound> sending = new Queue<Outbound>();
        const int MaximumQueuedPackets = 4096, MaximumQueuedBytes = 8 * 1024 * 1024;
        int sendingBytes;
        int generation;
        public bool Connected { get; private set; }
        public bool Connecting { get; private set; }
        public Action Opened;
        public Action<bool> Ended;
        public Action<ProtocolPacket> Handshake, Packet;
        public Action Changed;
        public Action<Exception> Error;
        public WebSessionCore(bool primary, IWireTransport wire) { Codec = new ProtocolCodec(primary); this.wire = wire; }
        public void Connect(string host, int port)
        {
            if (Connected || Connecting) return;
            Codec.Reset(); ClearSending(); Connecting = true;
            int token = ++generation; Notify();
            try
            {
                wire.Open(host, port, delegate
                {
                    if (token != generation || !Connecting) return;
                    Connecting = false; Connected = true; Notify();
                    try { wire.Send(Codec.Encode(-27, null)); if (Opened != null) Opened(); }
                    catch (Exception ex) { Fail(token, ex); }
                }, delegate(byte[] bytes) { Receive(token, bytes); }, delegate { Fail(token, null); });
            }
            catch (Exception ex) { Fail(token, ex); }
        }
        void Receive(int token, byte[] bytes)
        {
            if (token != generation || !Connected) return;
            try
            {
                Codec.Append(bytes); ProtocolPacket packet;
                while (token == generation && Connected && Codec.TryRead(out packet))
                {
                    if (packet.Command == -27)
                    {
                        if (Codec.HasKey || packet.Data.Length < 2) throw new InvalidOperationException("Invalid key handshake");
                        int length = packet.Data[0];
                        if (length <= 0 || packet.Data.Length < length + 1) throw new InvalidOperationException("Invalid key length");
                        var key = new byte[length];
                        for (int i = 0; i < length; i++) key[i] = unchecked((byte)packet.Data[i + 1]);
                        for (int i = 1; i < length; i++) key[i] ^= key[i - 1];
                        Codec.InstallKey(key); Notify();
                        if (Handshake != null) Handshake(packet);
                        Flush();
                    }
                    else { Notify(); if (Packet != null) Packet(packet); }
                }
            }
            catch (Exception ex) { Fail(token, ex); }
        }
        void DrainPending(int token)
        {
            if (token != generation) return;
            try
            {
                ProtocolPacket packet;
                while (token == generation && Codec.TryRead(out packet))
                {
                    if (packet.Command == -27)
                    {
                        if (Codec.HasKey || packet.Data.Length < 2) throw new InvalidOperationException("Invalid key handshake on drain");
                        int length = packet.Data[0];
                        if (length <= 0 || packet.Data.Length < length + 1) throw new InvalidOperationException("Invalid key length on drain");
                        var key = new byte[length];
                        for (int i = 0; i < length; i++) key[i] = unchecked((byte)packet.Data[i + 1]);
                        for (int i = 1; i < length; i++) key[i] ^= key[i - 1];
                        Codec.InstallKey(key); Notify();
                        if (Handshake != null) Handshake(packet);
                        Flush();
                    }
                    else { Notify(); if (Packet != null) Packet(packet); }
                }
            }
            catch (Exception ex) { if (Error != null) Error(ex); }
        }
        void Notify() { if (Changed != null) Changed(); }
        void Fail(int token, Exception error)
        {
            if (token != generation || (!Connected && !Connecting)) return;
            bool wasConnected = Connected;
            DrainPending(token);
            Close();
            if (error != null && Error != null) Error(error);
            if (Ended != null) Ended(wasConnected);
        }
        public void Send(sbyte command, sbyte[] data)
        {
            // Match the native sender's queue until encryption is negotiated. Copy mutable writers.
            try
            {
                int length = data == null ? 0 : data.Length;
                if (length > 65535) throw new ArgumentException("Outbound protocol payload exceeds 65535 bytes");
                int bytes = length + 3;
                if (sending.Count >= MaximumQueuedPackets || bytes > MaximumQueuedBytes - sendingBytes)
                    throw new InvalidOperationException("Outbound protocol queue exceeded");
                sending.Enqueue(new Outbound { Command = command, Data = data == null ? null : (sbyte[])data.Clone() });
                sendingBytes += bytes; Flush();
            }
            catch (Exception ex)
            {
                if (Connected || Connecting) Fail(generation, ex);
                else { ClearSending(); if (Error != null) Error(ex); }
            }
        }
        void Flush()
        {
            while (Connected && Codec.HasKey && sending.Count != 0)
            {
                var message = sending.Dequeue(); sendingBytes -= (message.Data == null ? 0 : message.Data.Length) + 3;
                wire.Send(Codec.Encode(message.Command, message.Data)); Notify();
            }
        }
        public void ClearSending() { sending.Clear(); sendingBytes = 0; }
        public void Close()
        {
            ++generation; Connected = Connecting = false;
            wire.Close(); ClearSending(); Codec.Reset(); Notify();
        }
    }
}
