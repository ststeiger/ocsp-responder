

namespace OCSPGuardian
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    public class HostConfigurator
    {


        private static void ConfigureHost(Microsoft.Extensions.Hosting.IHostBuilder hostBuilder)
        {
            Microsoft.Extensions.Hosting.SystemdHostBuilderExtensions.UseSystemd(hostBuilder);
        } // End Sub ConfigureHost 


        private static void ConfigureWebHost(
            Microsoft.AspNetCore.Hosting.IWebHostBuilder webHostBuilder,
            Microsoft.Extensions.Configuration.IConfigurationManager configuration,
            bool isWindows
        )
        {
            if (isWindows)
            {
                // webHostBuilder.UseIIS();
                webHostBuilder.UseIISIntegration();
            } // End if (isWindows) 


            string location = "/etc/COR/skynet/skynet.pfx";
            if (isWindows)
                location = @"D:\lolbot\CORaaaa\skynet\skynet.pfx";

            CertificateInfoDotNet cert = DotNetCertificateLoader.LoadPfxCertificate(location, "")!;
            

            libWebAppBasics.PseudoUrl url = configuration.GetValue<string?>("Kestrel:EndPoints:Https:Url", null)!;
            int listenPort = 5667;
            if (url != null)
                listenPort = url.Port;

            // Without calling UseKestrel, the application would not run on Kestrel.
            webHostBuilder.UseKestrel(
                delegate (Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions serverOptions)
                {
                    // serverOptions.Limits.MaxConcurrentConnections = 100;
                    // serverOptions.Limits.MaxConcurrentUpgradedConnections = 100;
                    // serverOptions.Limits.MaxRequestBodySize = 52428800;
                    serverOptions.AddServerHeader = false;
                    serverOptions.AllowSynchronousIO = false;
 
                    serverOptions.ListenAnyIP(listenPort,
                    delegate (Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions)
                    {
                        listenOptions.UseHttps(cert.Certificate!);
                    }
                );
                }
            );

            // webHostBuilder.UseContentRoot(System.AppContext.BaseDirectory);
            // webHostBuilder.UseWebRoot("wwwroot");

            // useStartup is not supported anymore
            // webHostBuilder.UseStartup(typeof(MyStartup).Assembly.FullName!);
            // webHostBuilder.UseStartup<MyStartup>();

            webHostBuilder.UseUrls();
        } // End Sub ConfigureWebHost 


        public static void Configure(
            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder,
            Microsoft.Extensions.Configuration.IConfigurationManager configuration, 
            bool isWindows
        )
        {
            // https://developers.redhat.com/blog/2018/07/24/improv-net-core-kestrel-performance-linux/
            // https://github.com/redhat-developer/kestrel-linux-transport
            // webBuilder.UseLinuxTransport();

            ConfigureHost(builder.Host);
            ConfigureWebHost(builder.WebHost, configuration, isWindows);
        } // End Sub Configure 


    } // End Class HostConfigurator 


} // End Namespace 
