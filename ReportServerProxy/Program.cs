
namespace ReportServerProxy;


using Microsoft.AspNetCore.Builder; // for app.MapReverseProxy
using Microsoft.Extensions.DependencyInjection; // for Add* 
using Microsoft.Extensions.Logging; // for builder.Logging.* 

public class Program
{


    public static async System.Threading.Tasks.Task<int> Main(string[] args)
    {
        Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

        // Add services to the container.
        // builder.Services.AddRazorPages();

        builder.Services.AddReverseProxy()
            .LoadFromMemory(
                new Yarp.ReverseProxy.Configuration.RouteConfig[]
                {
                    new Yarp.ReverseProxy.Configuration.RouteConfig
                    {
                        RouteId = "proxyRoute",
                        ClusterId = "localCluster",
                        Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                        {
                            Path = "{**catch-all}"
                            // Path = "/ReportServer/{**catch-all}"
                        },
                        /*
                        // Configure transforms for this route
                        Transforms = new System.Collections.Generic.List<
                            System.Collections.Generic.Dictionary<string, string>
                        >()
                        {
                            // Path rewrite transformation
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "PathRemovePrefix", "/" },
                                { "PathPrefix", "/" }
                            },
                            // Cookie domain transformation
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "ResponseTransform", "true" },
                                { "ResponseCookieDomain", "localhost" },
                                { "ResponseCookieTransform", "true" }
                            },
                            // Add X-Forwarded headers
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "X-Forwarded", "true" },
                                { "Forwarded", "true" }
                            }
                        }
                        */
                    }
                },
                new Yarp.ReverseProxy.Configuration.ClusterConfig[]
                {
                    new Yarp.ReverseProxy.Configuration.ClusterConfig
                    {
                        ClusterId = "localCluster",
                        Destinations = new System.Collections.Generic.Dictionary<
                            string, Yarp.ReverseProxy.Configuration.DestinationConfig
                        >
                        {
                            {
                                "destination1", new Yarp.ReverseProxy.Configuration.DestinationConfig()
                                {
                                    // Address = "http://localhost:11434/"
                                    Address = "https://reportserver.example.com"
                                }
                            }
                        }
                    }
                }
            )
            /*
            // Configure transforms using the fluent API instead of dictionaries
            .AddTransforms(builderContext =>
            {
                // Add forwarded headers
                builderContext.AddRequestTransform(transform =>
                {
                    transform.ProxyRequest.Headers.Remove("Host");

                    transform.ProxyRequest.Headers.Add("Host", "reportserver.example.com");

                    transform.ProxyRequest.Headers.Add("X-Forwarded-Host", transform.HttpContext.Request.Host.ToString());
                    transform.ProxyRequest.Headers.Add("X-Forwarded-Proto", transform.HttpContext.Request.Scheme);

                    // transform.RequestHeader.Append("X-Forwarded-Host", transform.HttpContext.Request.Host.ToString());
                    // transform.RequestHeader.Append("X-Forwarded-Proto", transform.HttpContext.Request.Scheme);
                    return System.Threading.Tasks.ValueTask.CompletedTask;
                });
                
                // Handle cookie domain rewriting
                builderContext.AddResponseTransform(async transform =>
                {
                    if (transform.ProxyResponse == null) return;

                    System.Net.Http.Headers.HttpResponseHeaders headers = transform.ProxyResponse.Headers;

                    // --- Manual Location Header Rewrite ---
                    // Check if the response contains a Location header (indicates a redirect)
                    if (headers.TryGetValues("Location", out var locationValues))
                    {
                        // Redirects usually have only one Location header value
                        string originalLocation = locationValues.FirstOrDefault();

                        if (!string.IsNullOrEmpty(originalLocation))
                        {
                            // Try to parse the original Location header value as a URI
                            // Use UriKind.Absolute because redirect locations are typically absolute URLs
                            if (Uri.TryCreate(originalLocation, UriKind.Absolute, out Uri originalUri))
                            {
                                try
                                {
                                    // Get the scheme (http) and host:port (localhost:12434) from the incoming request
                                    string proxyScheme = transform.HttpContext.Request.Scheme;
                                    Microsoft.AspNetCore.Http.HostString proxyHost = transform.HttpContext.Request.Host; // This includes host and port

                                    // Use UriBuilder to easily reconstruct the URI
                                    // Initialize with the original URI to keep path, query, fragment etc.
                                    UriBuilder newUriBuilder = new UriBuilder(originalUri);

                                    // Set the scheme and host based on the incoming request (proxy)
                                    newUriBuilder.Scheme = proxyScheme;
                                    newUriBuilder.Host = proxyHost.Host; // Use the hostname part (e.g., "localhost")

                                    // Set the port based on the incoming request
                                    if (proxyHost.Port.HasValue)
                                    {
                                        newUriBuilder.Port = proxyHost.Port.Value; // Use the incoming port (e.g., 12434)
                                    }
                                    else
                                    {
                                        // If no port was specified in the incoming host header, set the default port
                                        newUriBuilder.Port = proxyScheme == "https" ? 443 : 80;
                                    }

                                    // newUriBuilder automatically keeps the Path, Query, Fragment from originalUri

                                    Uri newUri = newUriBuilder.Uri;

                                    // Remove the original Location header
                                    headers.Remove("Location");

                                    // Add the rewritten Location header
                                    // Use OriginalString to preserve any encoding
                                    headers.Add("Location", newUri.OriginalString);
                                }
                                catch (Exception ex)
                                {
                                    // Log any errors that occur during URI manipulation
                                    // Consider setting up proper logging (e.g., using builder.Logging)
                                    // for production scenarios.
                                    System.Diagnostics.Debug.WriteLine($"Error rewriting Location header: {ex.Message}");
                                }
                            }
                            // If Uri.TryCreate failed, the original location wasn't a valid absolute URI - maybe log this?
                        }
                        // If originalLocation was null or empty - maybe log this?
                    }
                    // --- End Manual Location Header Rewrite ---


                    if (headers.Contains("Set-Cookie"))
                    {
                        System.Collections.Generic.List<string> cookies = 
                        headers.GetValues("Set-Cookie").ToList();
                        headers.Remove("Set-Cookie");

                        for (int i = 0; i < cookies.Count; i++)
                        {
                            string cookie = cookies[i];
                            // https://reportserver.example.com
                            // Replace example.com with localhost:12434
                            cookie = cookie.Replace("domain=reportserver.example.com", "domain=localhost:12434")
                                           .Replace("Domain=reportserver.example.com", "Domain=localhost:12434");
                            cookies[i] = cookie;
                        }

                        foreach (string cookie in cookies)
                        {
                            transform.ProxyResponse.Headers.Add("Set-Cookie", cookie);
                            // transform.ResponseHeader.Append("Set-Cookie", cookie);
                        }
                    }
                });
            })*/;



        builder.Services.AddSingleton<Yarp.ReverseProxy.Transforms.Builder.ITransformProvider, LoggingTransformProvider>();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddFilter("Yarp.ReverseProxy", Microsoft.Extensions.Logging.LogLevel.Warning);  // Suppress info/debug
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", Microsoft.Extensions.Logging.LogLevel.Warning);

        Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

        /*
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        app.UseRouting();
        app.UseAuthorization();
        
        app.MapRazorPages();
        app.Run();
        */

        app.MapReverseProxy();

        // app.Run("http://localhost:12434");
        await app.RunAsync("http://localhost:12434");

        return 0;
    } // End Task Main 


} // End Class Program 
