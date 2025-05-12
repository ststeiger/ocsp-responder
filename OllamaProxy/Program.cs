
namespace OllamaProxy;


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
                        }
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
                                    Address = "http://localhost:11434/"
                                } 
                            }
                        }
                    }
                }
            );

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
