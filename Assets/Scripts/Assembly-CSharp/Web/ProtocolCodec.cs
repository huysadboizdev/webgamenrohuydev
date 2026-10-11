using System;
namespace NroWeb
{
    public sealed class ProtocolPacket { public sbyte Command; public sbyte[] Data; public bool ItemOptionsInt32, Inventory200; }
    public sealed class ProtocolCodec
    {
        const int MaximumBufferedBytes = 0x2000000;
        readonly bool primary;
        byte[] buffer = new byte[1024], key;
        int start, end, readCursor, writeCursor;
        public bool Inventory200 { get; private set; }
        public bool ItemOptionsInt32 { get; private set; }
        public bool HasKey { get { return key != null; } }
        public int ReceivedBytes { get; private set; }
        public int SentBytes { get; private set; }
        public ProtocolCodec(bool primary) { this.primary = primary; }
        public void Append(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            int count = end - start;
            if (bytes.Length > MaximumBufferedBytes - count) throw new InvalidOperationException("Protocol receive buffer exceeded");
            if (buffer.Length - end < bytes.Length)
            {
                if (buffer.Length < count + bytes.Length) Array.Resize(ref buffer, Math.Max(count + bytes.Length, Math.Min(MaximumBufferedBytes, buffer.Length * 2)));
                Buffer.BlockCopy(buffer, start, buffer, 0, count); start = 0; end = count;
            }
            Buffer.BlockCopy(bytes, 0, buffer, end, bytes.Length); end += bytes.Length;
        }
        byte Decode(byte value, ref int cursor)
        {
            if (key == null) return value;
            byte decoded = (byte)(value ^ key[cursor]); cursor = (cursor + 1) % key.Length; return decoded;
        }
        bool Extended(sbyte command)
        {
            return command == -32 || command == -66 || command == 11 || command == -67 || command == -74 || command == -87 ||
                (primary && (command == 66 || command == 12)) ||
                (Inventory200 && (command == -30 || command == -35 || command == -36 || command == -86 || command == -44));
        }
        public bool TryRead(out ProtocolPacket packet)
        {
            packet = null;
            if (end - start < 3) return false;
            // Speculative header reads use a local cursor. Commit only when the entire frame is present.
            int cursor = readCursor, position = start;
            sbyte command = unchecked((sbyte)Decode(buffer[position++], ref cursor));
            bool extended = Extended(command); int header = extended ? 4 : 3;
            if (end - start < header) return false;
            int length;
            if (extended)
            {
                int low = unchecked((sbyte)Decode(buffer[position++], ref cursor)) + 128;
                int middle = unchecked((sbyte)Decode(buffer[position++], ref cursor)) + 128;
                int high = unchecked((sbyte)Decode(buffer[position++], ref cursor)) + 128;
                length = low | (middle << 8) | (high << 16);
            }
            else length = (Decode(buffer[position++], ref cursor) << 8) | Decode(buffer[position++], ref cursor);
            if (end - position < length) return false;
            var data = new sbyte[length];
            for (int i = 0; i < length; i++) data[i] = unchecked((sbyte)Decode(buffer[position++], ref cursor));
            start = position; readCursor = cursor; ReceivedBytes += header + length;
            if (start == end) start = end = 0;
            if (command == -29 && length >= 2 && data[0] == 100)
            {
                ItemOptionsInt32 = data[1] != 0; Inventory200 = length >= 3 && data[2] != 0;
            }
            packet = new ProtocolPacket { Command = command, Data = data, ItemOptionsInt32 = ItemOptionsInt32, Inventory200 = Inventory200 };
            return true;
        }
        public void InstallKey(byte[] value)
        {
            if (value == null || value.Length == 0 || value.Length > 127) throw new ArgumentException("Invalid protocol key");
            key = (byte[])value.Clone(); readCursor = writeCursor = 0;
        }
        public void Reset()
        {
            start = end = readCursor = writeCursor = 0; key = null;
            Inventory200 = ItemOptionsInt32 = false; ReceivedBytes = SentBytes = 0;
        }
        public sbyte ReadKey(sbyte value) { return unchecked((sbyte)Decode(unchecked((byte)value), ref readCursor)); }
        public sbyte WriteKey(sbyte value) { return unchecked((sbyte)Decode(unchecked((byte)value), ref writeCursor)); }
        public byte[] Encode(sbyte command, sbyte[] payload)
        {
            int length = payload == null ? 0 : payload.Length;
            if (length > 65535) throw new ArgumentException("Outbound protocol payload exceeds 65535 bytes");
            var bytes = new byte[3 + length]; bytes[0] = unchecked((byte)command);
            // Native client writes pre-key length with BinaryWriter ushort (little endian).
            bytes[1] = key == null ? (byte)length : (byte)(length >> 8);
            bytes[2] = key == null ? (byte)(length >> 8) : (byte)length;
            if (payload != null) Buffer.BlockCopy(payload, 0, bytes, 3, length);
            for (int i = 0; i < bytes.Length; i++) bytes[i] = Decode(bytes[i], ref writeCursor);
            SentBytes += bytes.Length; return bytes;
        }
    }
}
