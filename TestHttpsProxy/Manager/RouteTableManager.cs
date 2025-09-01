
namespace TestHttpsProxy
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    

    public static class RouteTableManager 
    {


        public static async System.Threading.Tasks.Task Start(string[] args)
        {
            RouteTable routeTable = new RouteTable();
            WebSocketNotifier notifier = new WebSocketNotifier();

            // --- Start proxy in background ---
            _ = System.Threading.Tasks.Task.Run(() => RunProxyAsync(routeTable));

            // --- Start admin web interface ---
            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // REST: list routes
            app.MapGet("/routes", () =>
            {
                System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Net.IPEndPoint>> routes = 
                routeTable.GetAll();

                System.Collections.Generic.List<object> mappedRoutes = new System.Collections.Generic.List<object>();
                foreach (System.Collections.Generic.KeyValuePair<string, System.Net.IPEndPoint> r in routes)
                {
                    mappedRoutes.Add(new { Host = r.Key, Address = r.Value.ToString() });
                }

                return Microsoft.AspNetCore.Http.Results.Json(mappedRoutes);
            });


            // REST: add/update a route
            app.MapPost("/routes", async (Microsoft.AspNetCore.Http.HttpContext ctx) =>
            {
                System.Collections.Generic.Dictionary<string, string>? json = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Collections.Generic.Dictionary<string, string>>(ctx.Request.Body);
                if (json == null || !json.TryGetValue("host", out string? host) || !json.TryGetValue("backend", out string? backend))
                    return Microsoft.AspNetCore.Http.Results.BadRequest("host and backend required");

                string[] parts = backend.Split(':');
                if (parts.Length != 2 || !System.Net.IPAddress.TryParse(parts[0], out System.Net.IPAddress? ip) 
                || !int.TryParse(parts[1], out int port))
                    return Microsoft.AspNetCore.Http.Results.BadRequest("backend must be ip:port");

                System.Net.IPEndPoint ep = new System.Net.IPEndPoint(ip, port);
                routeTable.AddOrUpdate(host, ep);

                await notifier.BroadcastAsync(new { action = "update", host, backend });
                return Microsoft.AspNetCore.Http.Results.Ok(new { host, backend });
            });

            // WebSocket: subscribe to updates
            app.MapGet("/ws", async (Microsoft.AspNetCore.Http.HttpContext ctx) =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    System.Net.WebSockets.WebSocket ws = await ctx.WebSockets.AcceptWebSocketAsync();
                    notifier.Add(ws);
                    await System.Threading.Tasks.Task.Delay(-1); // keep connection alive
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                }
            });

            app.UseWebSockets();
            await app.RunAsync("http://127.0.0.1:5000");
        }

        // --- Simplified proxy core ---
        static async System.Threading.Tasks.Task RunProxyAsync(RouteTable routeTable)
        {
            System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 443);
            listener.Start();
            System.Console.WriteLine("Proxy listening on 0.0.0.0:443");

            while (true)
            {
                System.Net.Sockets.TcpClient client = await listener.AcceptTcpClientAsync();
                _ = System.Threading.Tasks.Task.Run(() => HandleClientAsync(client, routeTable));
            }
        }

        static async System.Threading.Tasks.Task HandleClientAsync(System.Net.Sockets.TcpClient client, RouteTable routeTable)
        {
            using System.Net.Sockets.NetworkStream clientStream = client.GetStream();

            // TODO: Peek SNI or detect SSH here...
            string sniHost = await DetectSniOrSshAsync(clientStream);

            if (sniHost != null && routeTable.TryGet(sniHost, out System.Net.IPEndPoint? backend))
            {
                using System.Net.Sockets.TcpClient backendClient = new System.Net.Sockets.TcpClient();
                await backendClient.ConnectAsync(backend.Address, backend.Port);

                using System.Net.Sockets.NetworkStream backendStream = backendClient.GetStream();

                // Optional: write PROXY protocol header here
                // await ProxyProtocolV2.WriteProxyHeaderAsync(backendStream, (IPEndPoint)client.Client.RemoteEndPoint, backend);

                // Start bidirectional copy
                System.Threading.Tasks.Task t1 = clientStream.CopyToAsync(backendStream);
                System.Threading.Tasks.Task t2 = backendStream.CopyToAsync(clientStream);
                await System.Threading.Tasks.Task.WhenAny(t1, t2);
            }
            else
            {
                client.Close();
            }
        }

        static async System.Threading.Tasks.Task<string?> DetectSniOrSshAsync(System.Net.Sockets.NetworkStream stream)
        {
            // TODO: plug in your PeekByte/PeekClientHello logic from before
            await System.Threading.Tasks.Task.Yield();
            return "example.com"; // dummy for now
        }


    } // End Class RouteTableManager 


} // End Namespace 
