
namespace TestHttpsProxy
{


    public static class QuicPacketReader
    {


        public static QuicPacket? Parse(byte[] data)
        {
            if (data.Length < 1)
                return null;

            QuicPacket packet = new QuicPacket();
            int offset = 0;

            packet.FirstByte = data[offset++];
            packet.IsLongHeader = (packet.FirstByte & 0x80) != 0;

            if (!packet.IsLongHeader)
            {
                // Short header (1-RTT)
                packet.Payload = new byte[data.Length - offset];
                System.Array.Copy(data, offset, packet.Payload, 0, packet.Payload.Length);
                return packet;
            } // End if (!packet.IsLongHeader) 

            // Long header
            if (data.Length < 7) // minimal long header: first byte + 4 version + 2 cid lengths
                return null;

            packet.Version = (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
            offset += 4;

            int dcidLen = data[offset++];
            if (offset + dcidLen > data.Length) return null;
            packet.DestinationConnectionId = new byte[dcidLen];
            System.Array.Copy(data, offset, packet.DestinationConnectionId, 0, dcidLen);
            offset += dcidLen;

            int scidLen = data[offset++];
            if (offset + scidLen > data.Length) return null;
            packet.SourceConnectionId = new byte[scidLen];
            System.Array.Copy(data, offset, packet.SourceConnectionId, 0, scidLen);
            offset += scidLen;

            // Token for Initial packet
            if ((packet.FirstByte & 0x30) >> 4 == 0) // Initial packet type
            {
                int tokenLen = ReadVarInt(data, ref offset);
                if (offset + tokenLen > data.Length) return null;
                packet.Token = new byte[tokenLen];
                System.Array.Copy(data, offset, packet.Token, 0, tokenLen);
                offset += tokenLen;
            } // End if ((packet.FirstByte & 0x30) >> 4 == 0) // Initial packet type 

            // Remaining bytes are payload
            packet.Payload = new byte[data.Length - offset];
            System.Array.Copy(data, offset, packet.Payload, 0, packet.Payload.Length);

            // Determine packet type (Initial=0, 0-RTT=1, Handshake=2, Retry=3)
            packet.PacketType = (byte)((packet.FirstByte & 0x30) >> 4);

            return packet;
        } // End Function Parse 


        // Minimal QUIC varint reader (simplified, does not check errors)
        private static int ReadVarInt(byte[] data, ref int offset)
        {
            if (offset >= data.Length) return 0;
            byte b = data[offset];
            int length = 1 << (b >> 6);
            if (offset + length > data.Length) return 0;

            int value = b & 0x3F;
            for (int i = 1; i < length; i++)
            {
                value = (value << 8) | data[offset + i];
            } // Next i 

            offset += length;
            return value;
        } // End Function ReadVarInt 


    } // End Static Class QuicPacketReader 


} // End Namespace 
