
namespace TestWebSocketProxy
{


    public class SimpleRelay 
    {


        public static async System.Threading.Tasks.Task Relay(
            Microsoft.AspNetCore.Http.HttpContext context,
            System.Net.WebSockets.WebSocket source, 
            System.Net.WebSockets.WebSocket dest
        )
        {
            byte[] buffer = new byte[8192];
            while (source.State == System.Net.WebSockets.WebSocketState.Open &&
                   dest.State == System.Net.WebSockets.WebSocketState.Open
            )
            {
                System.Net.WebSockets.WebSocketReceiveResult result = await source.ReceiveAsync(
                    buffer, 
                    context.RequestAborted
                );

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    await dest.CloseAsync(
                        result.CloseStatus ?? System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription, 
                        context.RequestAborted
                    );

                    break;
                }

                await dest.SendAsync(new System.ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType, result.EndOfMessage, context.RequestAborted);
            } // Whend 

        } // End Task Relay 


        public static async System.Threading.Tasks.Task Test(Microsoft.AspNetCore.Http.HttpContext context)
        {
            using System.Net.WebSockets.ClientWebSocket clientSocket = new System.Net.WebSockets.ClientWebSocket();
            System.Net.WebSockets.WebSocket incomingSocket = await context.WebSockets.AcceptWebSocketAsync();
            System.Net.WebSockets.WebSocket outgoingSocket = clientSocket;

            // 👉 But ConnectAsync exists only on ClientWebSocket, not on the base class WebSocket.
            // That’s why the compiler error says "WebSocket" contains no definition for 'ConnectAsync'.
            await clientSocket.ConnectAsync(new System.Uri("ws://backend-server:5001/ws"), context.RequestAborted);

            System.Threading.Tasks.Task t1 = Relay(context, incomingSocket, outgoingSocket);
            System.Threading.Tasks.Task t2 = Relay(context,outgoingSocket, incomingSocket);
            await System.Threading.Tasks.Task.WhenAny(t1, t2);
        } // End Task Test 


    } // End Class SimpleRelay 


} // End Namespace 
