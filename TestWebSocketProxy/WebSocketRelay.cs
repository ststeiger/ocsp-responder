namespace TestWebSocketProxy
{


    public class WebSocketRelay 
    {


        // ---- Helpers ----
        public static async System.Threading.Tasks.Task RelayWebSocket(
            System.Net.WebSockets.WebSocket source,
            System.Net.WebSockets.WebSocket destination,
            System.Threading.CancellationToken cancel)
        {
            // Use a pooled buffer to reduce allocations
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(64 * 1024);
            try
            {
                while (!cancel.IsCancellationRequested &&
                       source.State == System.Net.WebSockets.WebSocketState.Open &&
                       destination.State == System.Net.WebSockets.WebSocketState.Open)
                {

#if NET8_0_OR_GREATER
                    System.Net.WebSockets.WebSocketReceiveResult recv = await source.ReceiveAsync(buffer, cancel);
                    if (recv.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        await destination.CloseAsync(source.CloseStatus ?? System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                            source.CloseStatusDescription, cancel);
                        break;
                    }

                    await destination.SendAsync(new System.ReadOnlyMemory<byte>(buffer, 0, recv.Count),
                        recv.MessageType, recv.EndOfMessage, cancel);
#else
                    System.Net.WebSockets.WebSocketReceiveResult r = await source.ReceiveAsync(
                        new System.ArraySegment<byte>(buffer),
                        cancel
                    );

                    if (r.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        await destination.CloseAsync(
                            source.CloseStatus ?? System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                            source.CloseStatusDescription,
                            cancel
                        );

                        break;
                    }

                    await destination.SendAsync(
                        new System.ArraySegment<byte>(buffer, 0, r.Count),
                        r.MessageType,
                        r.EndOfMessage,
                        cancel
                    );
#endif
                }
            }
            catch (System.OperationCanceledException)
            {
                // normal on shutdown
            }
            catch
            {
                // If either side blows up, attempt to close the other cleanly
                try
                {
                    if (destination.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        await destination.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError,
                            "Proxy relay error", System.Threading.CancellationToken.None);
                    }
                }
                catch { /* ignore */ }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        } // End Task RelayWebSocket 


    } // End Class WebSocketRelay 


} // End Namespace 
