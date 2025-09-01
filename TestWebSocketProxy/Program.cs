
namespace TestWebSocketProxy
{


    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;



    public class Program
    {


        public static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();



            // Configure WebSocket server settings
            app.UseWebSockets(new Microsoft.AspNetCore.Builder.WebSocketOptions
            {
                KeepAliveInterval = System.TimeSpan.FromSeconds(30)
                // ,ReceiveBufferSize = 64 * 1024
            });

            // Environment variable with backend base (required)
            string? targetBaseEnv = System.Environment.GetEnvironmentVariable("TARGET_WS_BASE");
            if (string.IsNullOrWhiteSpace(targetBaseEnv))
            {
                app.MapGet("/", (Microsoft.AspNetCore.Http.HttpContext ctx) =>
                {
                    ctx.Response.StatusCode = 500;
                    return ctx.Response.WriteAsync("Set TARGET_WS_BASE (e.g., ws://localhost:5001)");
                });
                await app.RunAsync();
                return 0;
            }

            System.Uri targetBaseUri = new System.Uri(targetBaseEnv, System.UriKind.Absolute);

            // Catch-all route (you can scope to /ws/{**catch-all} if you prefer)
            app.Map("/{**path}", async (Microsoft.AspNetCore.Http.HttpContext context) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    // Not a WebSocket request; you could forward HTTP here if you want.
                    context.Response.StatusCode = 426; // Upgrade Required
                    await context.Response.WriteAsync("WebSocket endpoint.");
                    return;
                }

                // Build the backend target URI by preserving path & query
                System.ReadOnlySpan<char> baseSpan = targetBaseUri.GetLeftPart(System.UriPartial.Authority).AsSpan();
                string baseStr = baseSpan.ToString();
                string forwardPath = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
                string query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value! : string.Empty;

                // Ensure we don't end up with double slashes
                string combined = baseStr.TrimEnd('/') + "/" + forwardPath.TrimStart('/');
                System.Uri targetUri = new System.Uri(combined + query, System.UriKind.Absolute);

                // Collect requested subprotocols from the client
                System.Collections.Generic.List<string> requestedSubprotocols = new System.Collections.Generic.List<string>();

                // Sec-WebSocket-Protocol: chat, superchat
                if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out Microsoft.Extensions.Primitives.StringValues subProtoValues))
                {
                    foreach (string? v in subProtoValues)
                    {
                        if (v == null) continue; // TODO: Is this correct ?

                        // Header may have a comma-separated list
                        string[] parts = v.Split(
                            ',', 
                            System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries
                        );

                        foreach (string p in parts)
                        {
                            if (!string.IsNullOrWhiteSpace(p))
                            {
                                requestedSubprotocols.Add(p);
                            }
                        } // Next p 

                    } // Next v 

                } // End if 

                // Connect to backend first so we know what subprotocol (if any) it accepted
                using System.Net.WebSockets.ClientWebSocket backend = new System.Net.WebSockets.ClientWebSocket();

                // Forward client headers that are safe/meaningful for WebSocket handshake
                // (Avoid hop-by-hop headers; ClientWebSocket handles the required Upgrade headers)
                foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers)
                {
                    // Skip forbidden headers for ClientWebSocket
                    // (ClientWebSocket will manage Host/Connection/Upgrade/Sec-* itself)
                    if (header.Key.StartsWith("Sec-WebSocket", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (header.Key.Equals("Connection", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (header.Key.Equals("Upgrade", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (header.Key.Equals("Host", System.StringComparison.OrdinalIgnoreCase)) continue;

                    try
                    {
                        backend.Options.SetRequestHeader(header.Key, header.Value.ToString());
                    }
                    catch
                    {
                        // Ignore invalid request headers for ClientWebSocket
                    }
                }

                // Offer same subprotocols to the backend
                foreach (string sp in requestedSubprotocols)
                {
                    try { backend.Options.AddSubProtocol(sp); } catch { /* ignore duplicates/invalid */ }
                }

                // Optional: Forward X-Forwarded-* info
                try
                {
                    string scheme = context.Request.Scheme;
                    string host = context.Request.Host.HasValue ? context.Request.Host.Value : string.Empty;
                    string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

                    backend.Options.SetRequestHeader("X-Forwarded-Proto", scheme);
                    backend.Options.SetRequestHeader("X-Forwarded-Host", host);
                    if (!string.IsNullOrEmpty(remoteIp))
                    {
                        backend.Options.SetRequestHeader("X-Forwarded-For", remoteIp);
                    }
                }
                catch { /* best-effort */ }

                // TLS handling if wss:// with custom certs would go here via backend.Options.ClientCertificates, RemoteCertificateValidationCallback, etc.

                try
                {
                    await backend.ConnectAsync(targetUri, context.RequestAborted);
                }
                catch (System.Exception ex)
                {
                    context.Response.StatusCode = 502;
                    await context.Response.WriteAsync("Backend connect failed: " + ex.Message);
                    return;
                }

                // Determine the negotiated subprotocol, if any
                string? agreedSubprotocol = backend.SubProtocol;

                // Accept the client WebSocket with the same subprotocol (if backend agreed)
                using System.Net.WebSockets.WebSocket client = await context.WebSockets.AcceptWebSocketAsync(agreedSubprotocol);

                // Relay in both directions
                System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                System.Threading.Tasks.Task relayTask1 = WebSocketRelay.RelayWebSocket(client, backend, cts.Token);
                System.Threading.Tasks.Task relayTask2 = WebSocketRelay.RelayWebSocket(backend, client, cts.Token);

                // When either direction completes, cancel the other and finish
                System.Threading.Tasks.Task completed = await System.Threading.Tasks.Task.WhenAny(relayTask1, relayTask2);
                cts.Cancel();

                try { await completed; } catch { /* swallow; the other direction likely closed */ }

                // Try to close the other if still open
                try
                {
                    if (client.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        await client.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Proxy shutdown", System.Threading.CancellationToken.None);
                    }
                }
                catch { /* ignore */ }

                try
                {
                    if (backend.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        await backend.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Proxy shutdown", System.Threading.CancellationToken.None);
                    }
                }
                catch { /* ignore */ }
            });



            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            await app.RunAsync();

            return 0;
        } // End Task Main 


    } // End Class Program 


} // End Namespace 
