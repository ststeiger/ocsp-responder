

namespace OCSPGuardian
{
    
    using Microsoft.AspNetCore.Hosting;
    

    public class HostConfigurator
    {


        private static void ConfigureHost(Microsoft.Extensions.Hosting.IHostBuilder hostBuilder)
        {
            Microsoft.Extensions.Hosting.SystemdHostBuilderExtensions.UseSystemd(hostBuilder);
        } // End Sub ConfigureHost 


        private static System.Security.Cryptography.X509Certificates.X509Certificate2? 
            LoadPemCertificate()
        {
            string pemKey = libWebAppBasics.SecretManager.GetSecretOrThrow<string>("skynet_key");
            string pemCert = libWebAppBasics.SecretManager.GetSecretOrThrow<string>("skynet_cert");

            System.Security.Cryptography.X509Certificates.X509Certificate2 ca = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(pemCert);

            System.Security.Cryptography.RSA rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(pemKey);
            System.Security.Cryptography.X509Certificates.X509Certificate2 b = System.Security.Cryptography.X509Certificates
                .RSACertificateExtensions.CopyWithPrivateKey(ca, rsa);

            if (b.HasPrivateKey)
                return b;

            return null;
        }


        private static void ConfigureWebHost(
            Microsoft.AspNetCore.Hosting.IWebHostBuilder webHostBuilder,
            Microsoft.Extensions.Configuration.IConfigurationManager configuration,
            bool isWindows
        )
        {
            bool usesIIS = (isWindows && System.Environment.GetEnvironmentVariable("APP_POOL_ID") is string) ? true : false;

            if (usesIIS)
            {
                // webHostBuilder.UseIIS();
                webHostBuilder.UseIISIntegration();
            } // End if (isWindows) 
            else
            {
                byte[] selfSignedCertificateData = SimpleChallengeResponder.SelfSigned.CreateSelfSignedCertificate("");


                libWebAppBasics.PseudoUrl url = Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<string>(configuration, "Kestrel:EndPoints:Https:Url", "")!;
                int listenPort = 5667;
                if (url != null && url.Port.HasValue)
                    listenPort = url.Port.Value;

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
                            // listenOptions.UseHttps(cert.Certificate!);
                            // listenOptions.UseHttps(caCert!);

                            System.Security.Cryptography.X509Certificates.X509Certificate2 cert =
                                new System.Security.Cryptography.X509Certificates.X509Certificate2(selfSignedCertificateData);
                            listenOptions.UseHttps(cert);
                        }
                    );
                    }
                );

                // webHostBuilder.UseContentRoot(System.AppContext.BaseDirectory);
                // webHostBuilder.UseWebRoot("wwwroot");

                // useStartup is not supported anymore
                // webHostBuilder.UseStartup(typeof(MyStartup).Assembly.FullName!);
                // webHostBuilder.UseStartup<MyStartup>();

            }

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
