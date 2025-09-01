
namespace TestHttpsProxy
{


    // Requires: dotnet add package StreamExtended
    internal static class ProxyTest 
    {

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, bool> _connections
            = new System.Collections.Concurrent.ConcurrentDictionary<int, bool>();


        public static async System.Threading.Tasks.Task RunAsync()
        {
            // --- Configuration ---
            // Listen address/port for incoming TLS clients:
            System.Net.IPAddress listenAddress = System.Net.IPAddress.Any;
            int listenPort = 8443; // change to 443 if you run with proper privileges
            bool withProxyV2 = true;

            // SNI hostname -> backend endpoint map (case-insensitive)
            System.Collections.Generic.Dictionary<string, System.Net.IPEndPoint> routes = 
                new System.Collections.Generic.Dictionary<string, System.Net.IPEndPoint>(
                System.StringComparer.OrdinalIgnoreCase)
            {
                // EXAMPLES — replace with your three domains and ports:
                { "one.example.com", new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6001) },
                { "two.example.com", new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6002) },
                //{ "three.example.com", new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6003) },
                // { "localhost", new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 10001) }, // DeltaCAD
                { "localhost", new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 7000) }, // GeekTools
            };


            // https://localhost:10001/Api/GetClassAtt


            // Optional default backend if SNI is missing or unknown. Set to null to reject.
            System.Net.IPEndPoint? defaultBackend = null; // e.g., new IPEndPoint(IPAddress.Loopback, 6060);

            // Connection/read/write timeouts (Milliseconds)
            int connectTimeoutMs = 10_000;
            int idleTimeoutMs = 120_000;

            System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
            System.Console.CancelKeyPress += delegate(object? sender, System.ConsoleCancelEventArgs e) 
            {
                e.Cancel = true;
                cts.Cancel();
            };

            System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(listenAddress, listenPort);
            listener.Server.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
            listener.Start();
            System.Console.WriteLine($"[proxy] listening on {listenAddress}:{listenPort}");

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    System.Net.Sockets.TcpClient client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    _ = HandleClientAsync(client, routes, defaultBackend, withProxyV2, connectTimeoutMs, idleTimeoutMs, cts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // shutting down
            }
            finally
            {
                listener.Stop();
            }

            System.Console.WriteLine("[proxy] stopped.");
        } // End Task RunAsync 



        private static int _nextId = 0;

        private static async System.Threading.Tasks.Task HandleClientAsync(
            System.Net.Sockets.TcpClient client,
            System.Collections.Generic.IDictionary<string, System.Net.IPEndPoint> routes,
            System.Net.IPEndPoint? defaultBackend,
            bool withProxyV2, 
            int connectTimeoutMs,
            int idleTimeoutMs,
            System.Threading.CancellationToken cancel)
        {
            int id = System.Threading.Interlocked.Increment(ref _nextId);
            _connections[id] = true;

            client.NoDelay = true;
            client.LingerState = new System.Net.Sockets.LingerOption(false, 0);
            client.ReceiveTimeout = idleTimeoutMs;
            client.SendTimeout = idleTimeoutMs;

            System.Console.WriteLine($"[#{id}] client connected from {client.Client.RemoteEndPoint}");

            await using System.Net.Sockets.NetworkStream clientNetStream = client.GetStream();

            // Use stream-extended to peek ClientHello (non-destructive) so we can still forward the exact bytes.
            StreamExtended.DefaultBufferPool bufferPool = new StreamExtended.DefaultBufferPool();
            await using StreamExtended.Network.CustomBufferedStream clientBuffered = 
                new StreamExtended.Network.CustomBufferedStream(clientNetStream, bufferPool, 4096
            );

            string? sni = null;
            try
            {
                // Peek first 8 bytes to detect protocol
                // byte[] probe = new byte[8];
                // int read = await clientBuffered.PeekBytesAsync(probe, 0, probe.Length, cancel);
                byte[] probe = await clientBuffered.PeekBytesAsync(7, cancel);
                
                // if (read >= 7 && probe[0] == (byte)'S' && probe[1] == (byte)'S' && probe[2] == (byte)'H')
                if (probe.Length >= 7 && probe[0] == (byte)'S' && probe[1] == (byte)'S' && probe[2] == (byte)'H')
                {
                    // SSH detected
                    // int destPort = 22;
                    // System.Net.IPAddress destSshAddress = System.Net.IPAddress.Loopback;
                    System.Net.IPAddress destSshAddress = System.Net.IPAddress.Parse("88.84.21.77");
                    int destPort = 443;

                    System.Net.IPEndPoint sshBackend = new System.Net.IPEndPoint(destSshAddress, destPort);
                    System.Console.WriteLine($"[#{id}] Detected SSH connection, forwarding to {sshBackend}");

                    await ForwardRawAsync(clientBuffered, sshBackend, connectTimeoutMs, idleTimeoutMs, id, cancel);
                    return;
                }


                StreamExtended.ClientHelloInfo hello = await StreamExtended.SslTools.PeekClientHello(clientBuffered, bufferPool)
                    .ConfigureAwait(false);

                if (hello != null && hello.Extensions != null)
                {

                    foreach (System.Collections.Generic.KeyValuePair<string, StreamExtended.Models.SslExtension> kvp in hello.Extensions)
                    {
                        if (string.Equals(kvp.Key, "server_name", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            sni = kvp.Value?.Data;
                            break;
                        }
                    }

                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[#{id}] error peeking ClientHello: {ex.Message}");
            }

            if (string.IsNullOrEmpty(sni))
            {
                if (defaultBackend == null)
                {
                    System.Console.WriteLine($"[#{id}] no SNI and no default backend configured. Closing.");
                    client.Close();
                    _connections.TryRemove(id, out _);
                    return;
                }
                System.Console.WriteLine($"[#{id}] no SNI -> default {defaultBackend}");
            }
            else
            {
                System.Console.WriteLine($"[#{id}] SNI: {sni}");
            }

            // Pick backend based on SNI (or default)
            System.Net.IPEndPoint backendEndpoint;
            if (!string.IsNullOrEmpty(sni) && routes.TryGetValue(sni!, out System.Net.IPEndPoint? mapped))
            {
                backendEndpoint = mapped;
            }
            else if (defaultBackend != null)
            {
                backendEndpoint = defaultBackend!;
            }
            else
            {
                System.Console.WriteLine($"[#{id}] unknown SNI '{sni}' and no default backend. Closing.");
                client.Close();
                _connections.TryRemove(id, out _);
                return;
            }

            // Connect to backend with timeout
            System.Net.Sockets.TcpClient backend = new System.Net.Sockets.TcpClient();
            backend.NoDelay = true;
            backend.LingerState = new System.Net.Sockets.LingerOption(false, 0);
            System.Threading.CancellationTokenSource connectCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancel);
            connectCts.CancelAfter(connectTimeoutMs);

            try
            {
                await backend.ConnectAsync(backendEndpoint.Address, backendEndpoint.Port, connectCts.Token).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
                System.Console.WriteLine($"[#{id}] backend connect timeout to {backendEndpoint}");
                client.Close();
                backend.Close();
                _connections.TryRemove(id, out _);
                return;
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[#{id}] backend connect failed to {backendEndpoint}: {ex.Message}");
                client.Close();
                backend.Close();
                _connections.TryRemove(id, out _);
                return;
            }

            System.Console.WriteLine($"[#{id}] connected to backend {backendEndpoint}");

            await using System.Net.Sockets.NetworkStream backendNetStream = backend.GetStream();

            // Relay both directions until either side closes or times out.
            System.Threading.CancellationTokenSource relayCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancel);


            if (withProxyV2)
            {
                await ProxyProtocolV2.WriteProxyHeaderAsync(
                    backendNetStream,
                    (System.Net.IPEndPoint)client.Client.RemoteEndPoint!,
                    backendEndpoint,
                    cancel
                );
            }

#if PROXY_V1_ENABLED
            bool withProxyV1 = false;
            if (withProxyV1)
            { 
                await ProxyProtocolV1.WriteProxyHeaderAsync(
                    backendNetStream,
                    (System.Net.IPEndPoint)client.Client.RemoteEndPoint!,
                    backendEndpoint,
                    cancel
                );
            }
#endif 

            System.Threading.Tasks.Task t1 = PumpAsync(clientBuffered, backendNetStream, idleTimeoutMs, $"[#{id}] c→b", relayCts.Token);
            System.Threading.Tasks.Task t2 = PumpAsync(backendNetStream, clientNetStream, idleTimeoutMs, $"[#{id}] b→c", relayCts.Token);

            // When either direction completes, cancel the other and close both sockets.
            System.Threading.Tasks.Task first = await System.Threading.Tasks.Task.WhenAny(t1, t2).ConfigureAwait(false);
            relayCts.Cancel();

            // Await both (ignore exceptions, we're shutting down anyway)
            try { await t1.ConfigureAwait(false); } catch { }
            try { await t2.ConfigureAwait(false); } catch { }

            try { client.Close(); } catch { }
            try { backend.Close(); } catch { }

            System.Console.WriteLine($"[#{id}] connection closed.");
            _connections.TryRemove(id, out _);
        } // End Function HandleClientAsync 


        // Forward ssh 
        private static async System.Threading.Tasks.Task ForwardRawAsync(
            System.IO.Stream clientBuffered,
            System.Net.IPEndPoint backendEndpoint,
            int connectTimeoutMs,
            int idleTimeoutMs,
            int id,
            System.Threading.CancellationToken cancel
        )
        {
            System.Net.Sockets.TcpClient backend = new System.Net.Sockets.TcpClient();
            System.Threading.CancellationTokenSource connectCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancel);
            connectCts.CancelAfter(connectTimeoutMs);

            await backend.ConnectAsync(backendEndpoint.Address, backendEndpoint.Port, connectCts.Token);

            await using System.Net.Sockets.NetworkStream backendStream = backend.GetStream();

            System.Threading.CancellationTokenSource relayCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancel);
            System.Threading.Tasks.Task t1 = PumpAsync(clientBuffered, backendStream, idleTimeoutMs, $"[#{id}] c→b", relayCts.Token);
            System.Threading.Tasks.Task t2 = PumpAsync(backendStream, clientBuffered, idleTimeoutMs, $"[#{id}] b→c", relayCts.Token);

            await System.Threading.Tasks.Task.WhenAny(t1, t2);
            relayCts.Cancel();
        } // End Task ForwardRawAsync 


        private static async System.Threading.Tasks.Task PumpAsync(
            System.IO.Stream src,
            System.IO.Stream dst,
            int idleTimeoutMs,
            string tag,
            System.Threading.CancellationToken cancel)
        {
            // Use a reasonable buffer; stream-extended's CustomBufferedStream will serve the peeked bytes first.
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(32 * 1024);
            try
            {
                System.Threading.CancellationTokenSource readCts = new System.Threading.CancellationTokenSource();
                System.Threading.CancellationTokenSource linked = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(
                    cancel, readCts.Token
                );

                while (!cancel.IsCancellationRequested)
                {
                    // Implement idle timeout manually with ReadAsync cancellation.
                    readCts.CancelAfter(idleTimeoutMs);
                    int n = 0;
                    try
                    {
                        n = await src.ReadAsync(System.MemoryExtensions.AsMemory(buffer, 0, buffer.Length), linked.Token)
                            .ConfigureAwait(false);
                    }
                    catch (System.OperationCanceledException)
                    {
                        if (cancel.IsCancellationRequested) break; // external cancel
                                                                   // idle timeout
                        System.Console.WriteLine($"{tag} idle timeout");
                        break;
                    }
                    finally
                    {
                        readCts.CancelAfter(System.Threading.Timeout.Infinite);
                    }

                    if (n <= 0) break; // EOF


                    await dst.WriteAsync(System.MemoryExtensions.AsMemory(buffer, 0, n), cancel).ConfigureAwait(false);
                    await dst.FlushAsync(cancel).ConfigureAwait(false);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"{tag} error: {ex.Message}");
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
                try 
                { 
                    dst.Flush(); 
                } 
                catch { }
            }

        } // End Task PumpAsync 


    } // End Class ProxyTest 


} // End Namespace 
