
namespace TestHttpsProxy
{


    public class QuicPacket
    {
        public byte FirstByte { get; set; }
        public bool IsLongHeader { get; set; }
        public byte PacketType { get; set; } // Initial, 0-RTT, Handshake, Retry
        public uint Version { get; set; }
        public byte[] DestinationConnectionId { get; set; } = System.Array.Empty<byte>();
        public byte[] SourceConnectionId { get; set; } = System.Array.Empty<byte>();
        public byte[] Token { get; set; } = System.Array.Empty<byte>(); // Optional, only for Initial
        public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    } // End Class QuicPacket 


} // End Namespace 
