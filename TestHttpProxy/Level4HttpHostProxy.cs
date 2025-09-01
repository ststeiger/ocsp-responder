
namespace TestHttpProxy
{


    class Level4HttpHostProxy
    {
        private const int ListenPort = 8080;
        private const int PeekBufferSize = 16384; // 16 KB peek buffer for large headers


        // Host -> backend mapping
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Net.IPEndPoint> hostMap =
            new System.Collections.Concurrent.ConcurrentDictionary<string, System.Net.IPEndPoint>(
                System.StringComparer.OrdinalIgnoreCase
            )
            {
                // http://localhost:12345/CAFM/w8/index.html
                // ["localhost"] = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8080),
                ["localhost"] = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 12345),
                // ["example1.local"] = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8080),
                // ["example2.local"] = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8081)
            };

        public static async System.Threading.Tasks.Task Test()
        {
            System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(
                System.Net.IPAddress.Any, 
                ListenPort
            );

            listener.Start();
            System.Console.WriteLine($"Level 4 HTTP proxy listening on port {ListenPort}...");

            while (true)
            {
                System.Net.Sockets.TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            } // Whend 

        } // End Task Test 

        private static async System.Threading.Tasks.Task HandleClientAsync(
            System.Net.Sockets.TcpClient client
        )
        {
            System.Net.Sockets.TcpClient? backend = null;

            try
            {
                System.Net.Sockets.NetworkStream clientStream = client.GetStream();
                byte[] peekBuffer = new byte[PeekBufferSize];

                int bytesRead = await ReadInitialHeaderAsync(clientStream, peekBuffer);
                if (bytesRead == 0)
                {
                    client.Close();
                    return;
                } // End if (bytesRead == 0) 

                string? host = ExtractHostHeader(peekBuffer, bytesRead);

                // Remove port if present 
                if (host != null)
                {
                    int ind = host.IndexOf(':');
                    if (ind != -1)
                        host = host.Substring(0, ind);
                } // End if (host != null) 


                if (host == null || !hostMap.TryGetValue(host, out System.Net.IPEndPoint? backendEndPoint))
                {
                    client.Close();
                    return;
                } // End if (host == null || !hostMap.TryGetValue(host, out System.Net.IPEndPoint? backendEndPoint))

                backend = new System.Net.Sockets.TcpClient();
                await backend.ConnectAsync(backendEndPoint.Address, backendEndPoint.Port);
                System.Net.Sockets.NetworkStream backendStream = backend.GetStream();

                // Forward the peeked bytes first
                await backendStream.WriteAsync(peekBuffer, 0, bytesRead);

                // Start bidirectional streaming
                System.Threading.Tasks.Task t1 = PipeAsync(clientStream, backendStream);
                System.Threading.Tasks.Task t2 = PipeAsync(backendStream, clientStream);

                await System.Threading.Tasks.Task.WhenAny(t1, t2);
            } // End Try 
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                backend?.Close();
            }
        } // End Task HandleClientAsync 


        private static async System.Threading.Tasks.Task<int> ReadInitialHeaderAsync(
            System.Net.Sockets.NetworkStream stream, 
            byte[] buffer
        )
        {
            int totalRead = 0;
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;

                // Simple heuristic: check for end of HTTP headers
                if (System.Text.Encoding.ASCII.GetString(buffer, 0, totalRead).Contains("\r\n\r\n")) break;

                if (totalRead == buffer.Length) break; // buffer full
            } // Whend 

            return totalRead;
        } // End Task ReadInitialHeaderAsync 

        private static string? ExtractHostHeader(byte[] buffer, int length)
        {
            string header = System.Text.Encoding.ASCII.GetString(buffer, 0, length);
            foreach (string line in header.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.None))
            {
                if (line.StartsWith("Host:", System.StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(5).Trim();
                }

            } // Next line 

            return null;
        } // End Function ExtractHostHeader 


        private static async System.Threading.Tasks.Task PipeAsync(
            System.Net.Sockets.NetworkStream from,
            System.Net.Sockets.NetworkStream to
        )
        {
            byte[] buffer = new byte[8192];
            try
            {
                int bytesRead;
                while ((bytesRead = await from.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await to.WriteAsync(buffer, 0, bytesRead);
                } // Whend 
            }
            catch { /* ignore */ }
        } // End Task PipeAsync 


    } // End Class Level4HttpHostProxy 


} // End Namespace 
