
namespace TestHttpsProxy
{


    // WebSocket broadcaster
    public class WebSocketNotifier
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<System.Net.WebSockets.WebSocket> _clients = 
            new System.Collections.Concurrent.ConcurrentBag<System.Net.WebSockets.WebSocket>();


        public void Add(System.Net.WebSockets.WebSocket ws) 
        {
            this._clients.Add(ws);
        } // End Sub Add 


        public async System.Threading.Tasks.Task BroadcastAsync(object obj)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(obj);
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

            foreach (System.Net.WebSockets.WebSocket ws in _clients)
            {
                if (ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await ws.SendAsync(
                        new System.ArraySegment<byte>(buffer),
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        true,
                        System.Threading.CancellationToken.None
                    );
                } // End if (ws.State == System.Net.WebSockets.WebSocketState.Open) 

            } // Next ws 

        } // End Task BroadcastAsync 


    } // End Class WebSocketNotifier 


} // End Namespace 
