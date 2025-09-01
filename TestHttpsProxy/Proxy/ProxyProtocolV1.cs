
namespace TestHttpsProxy
{


    public static class ProxyProtocolV1
    {


        /// <summary>
        /// Writes a PROXY protocol v1 header (text-based).
        /// </summary>
        public static async System.Threading.Tasks.Task WriteProxyHeaderAsync(
            System.IO.Stream stream,
            System.Net.IPEndPoint client,
            System.Net.IPEndPoint proxy,
            System.Threading.CancellationToken cancel = default
        )
        {
            string family;
            if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                family = "TCP4";
            }
            else if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                family = "TCP6";
            }
            else
            {
                family = "UNKNOWN";
            }

            string header;
            if (family == "UNKNOWN")
            {
                // just "PROXY UNKNOWN\r\n" (ignores address/ports)
                header = "PROXY UNKNOWN\r\n";
            }
            else
            {
                header = $"PROXY {family} {client.Address} {proxy.Address} {client.Port} {proxy.Port}\r\n";
            }

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancel).ConfigureAwait(false);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        } // End Task WriteProxyHeaderAsync 


    } // End Class ProxyProtocolV1 


} // End Namespace 
