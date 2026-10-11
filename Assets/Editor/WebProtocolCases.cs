using System;
using System.Collections.Generic;
using NroWeb;

public static class WebProtocolCases
{
    static void Check(bool value, string name) { if (!value) throw new Exception(name); }
    static byte[] Join(params byte[][] parts)
    {
        var result = new List<byte>(); foreach (var p in parts) result.AddRange(p); return result.ToArray();
    }
    // Independent server-side encoder; extended lengths use signed-byte offset, little endian.
    static byte[] Server(int command, byte[] payload, bool extended, byte[] key, ref int cursor)
    {
        var bytes = new List<byte>(); bytes.Add(unchecked((byte)command));
        if (extended) { bytes.Add(unchecked((byte)((payload.Length & 255) - 128))); bytes.Add(unchecked((byte)(((payload.Length >> 8) & 255) - 128))); bytes.Add(unchecked((byte)(((payload.Length >> 16) & 255) - 128))); }
        else { bytes.Add((byte)(payload.Length >> 8)); bytes.Add((byte)payload.Length); }
        bytes.AddRange(payload);
        if (key != null) for (int i = 0; i < bytes.Count; i++) { bytes[i] ^= key[cursor]; cursor = (cursor + 1) % key.Length; }
        return bytes.ToArray();
    }
    public static void Run()
    {
        var codec = new ProtocolCodec(true); ProtocolPacket packet; int cursor = 0;
        byte[] handshake = Server(-27, new byte[] { 3, 7, 14, 4 }, false, null, ref cursor);
        codec.Append(new byte[] { handshake[0], handshake[1] });
        Check(!codec.TryRead(out packet), "fragmented header must wait");
        codec.Append(new byte[] { handshake[2], handshake[3], handshake[4], handshake[5], handshake[6] });
        Check(codec.TryRead(out packet) && packet.Command == -27, "fragmented handshake must decode");
        codec.InstallKey(new byte[] { 7, 9, 13 });
        byte[] key = new byte[] { 7, 9, 13 };
        byte[] negotiation = Server(-29, new byte[] { 100, 1, 1 }, false, key, ref cursor);
        byte[] large = new byte[70000]; large[69999] = 42;
        byte[] inventory = Server(-30, large, true, key, ref cursor);
        byte[] legacy = Server(5, new byte[] { 99 }, false, key, ref cursor);
        byte[] all = Join(negotiation, inventory, legacy);
        // Every byte of the header and payload can arrive in a different websocket message.
        var packets = new List<ProtocolPacket>();
        for (int i = 0; i < all.Length; i++) { codec.Append(new byte[] { all[i] }); if (codec.TryRead(out packet)) packets.Add(packet); }
        Check(packets.Count == 3, "cipher cursor must roll back on incomplete frame");
        Check(packets[0].ItemOptionsInt32 && packets[0].Inventory200, "negotiation snapshot");
        Check(packets[1].Data.Length == 70000 && packets[1].Data[69999] == 42, "inventory extended length");
        Check(packets[2].Data[0] == 99, "combined frame remainder");
        var other = new ProtocolCodec(false); cursor = 0;
        other.Append(Server(-30, new byte[] { 1 }, false, null, ref cursor));
        Check(other.TryRead(out packet) && !packet.Inventory200 && !packet.ItemOptionsInt32, "session isolation and legacy inventory length");
        cursor = 0; other.Append(Server(-32, new byte[256], true, null, ref cursor));
        Check(other.TryRead(out packet) && packet.Data.Length == 256, "secondary always extended command");
        codec.Append(new byte[] { 1 }); codec.Reset();
        Check(!codec.TryRead(out packet) && !codec.Inventory200 && !codec.HasKey, "reconnect resets pending bytes/key/negotiation");
        Check(codec.Encode(-27, null)[0] == 229, "unencrypted handshake command");
        codec.InstallKey(key); byte[] encoded = codec.Encode(6, new sbyte[] { -1, 3 });
        Check(encoded.Length == 5 && encoded[0] == (6 ^ 7) && encoded[1] == 9 && encoded[2] == (2 ^ 13) && encoded[3] == (255 ^ 7), "outbound big endian encrypted framing");
        Check(codec.Encode(1, null)[0] == (1 ^ 13), "outbound XOR cursor continues");
        cursor = 0; var primary = new ProtocolCodec(true); primary.Append(Server(66, new byte[] { 8 }, true, null, ref cursor));
        Check(primary.TryRead(out packet) && packet.Data[0] == 8, "primary command 66 extended length");
        var secondary = new ProtocolCodec(false); cursor = 0; secondary.Append(Server(66, new byte[] { 8 }, false, null, ref cursor));
        Check(secondary.TryRead(out packet) && packet.Data[0] == 8, "secondary command 66 standard length");
        Console.WriteLine("PASS: fragmented handshake, XOR rollback, combined frames, negotiation snapshots, 70KB inventory, isolated sessions, reconnect, outbound cipher, primary/secondary framing");
    }
}
