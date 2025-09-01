
namespace TestHttpsProxy
{


    public static class ProxyProtocolV2
    {


        private static readonly byte[] Signature = new byte[]
        {
            0x0D, 0x0A, 0x0D, 0x0A,
            0x00, 0x0D, 0x0A, 0x51,
            0x55, 0x49, 0x54, 0x0A
        };


        /// <summary>
        /// Writes a PROXY protocol v2 header (TCP over IPv4/IPv6).
        /// </summary>
        public static async System.Threading.Tasks.Task WriteProxyHeaderAsync(
            System.IO.Stream stream,
            System.Net.IPEndPoint client,
            System.Net.IPEndPoint proxy,
            System.Threading.CancellationToken cancel = default)
        {
            // Version & Command: v2, PROXY
            byte verCmd = 0x20 | 0x01; // 0x20 = v2, 0x01 = PROXY

            // Address family + protocol
            byte famProto;
            byte[] srcAddr;
            byte[] dstAddr;

            if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                famProto = 0x11; // TCP over IPv4
                srcAddr = client.Address.GetAddressBytes();
                dstAddr = proxy.Address.GetAddressBytes();
            }
            else if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                famProto = 0x21; // TCP over IPv6
                srcAddr = client.Address.GetAddressBytes();
                dstAddr = proxy.Address.GetAddressBytes();
            }
            else
            {
                throw new System.NotSupportedException("Only IPv4/IPv6 supported");
            }

            // Build payload
            byte[] srcPort = System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)client.Port));
            byte[] dstPort = System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((short)proxy.Port));

            byte[] addrPart = new byte[srcAddr.Length + dstAddr.Length + 4];
            System.Buffer.BlockCopy(srcAddr, 0, addrPart, 0, srcAddr.Length);
            System.Buffer.BlockCopy(dstAddr, 0, addrPart, srcAddr.Length, dstAddr.Length);
            System.Buffer.BlockCopy(srcPort, 0, addrPart, srcAddr.Length + dstAddr.Length, 2);
            System.Buffer.BlockCopy(dstPort, 0, addrPart, srcAddr.Length + dstAddr.Length + 2, 2);

            // Length field (big endian)
            ushort length = (ushort)addrPart.Length;
            byte[] lenBytes = new byte[2] { (byte)(length >> 8), (byte)(length & 0xFF) };

            // Final header = signature + ver/cmd + fam/proto + length + addrPart
            byte[] header = new byte[Signature.Length + 1 + 1 + 2 + addrPart.Length];
            int pos = 0;
            System.Buffer.BlockCopy(Signature, 0, header, pos, Signature.Length); pos += Signature.Length;
            header[pos++] = verCmd;
            header[pos++] = famProto;
            System.Buffer.BlockCopy(lenBytes, 0, header, pos, 2); pos += 2;
            System.Buffer.BlockCopy(addrPart, 0, header, pos, addrPart.Length);

            // Write to backend
            await stream.WriteAsync(header, 0, header.Length, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        } // End Task WriteProxyHeaderAsync 


    } // End Class ProxyProtocolV2 


} // End Namespace 
