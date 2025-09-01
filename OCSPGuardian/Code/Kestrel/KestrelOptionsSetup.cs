
namespace OCSPGuardian
{
    
    using Microsoft.Extensions.DependencyInjection; // for GetRequiredService 
    

    // services.Configure<ProxyOptions>(Configuration.GetSection("Proxy"));
    public class ProxyOptions
    {
        public bool SslPassthrough { get; set; }
    }


    internal class KestrelOptionsSetup
        : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
    {

        protected readonly Microsoft.Extensions.Logging.ILogger<KestrelOptionsSetup> m_logger;
        private readonly ProxyOptions m_proxyOptions;
        private readonly libCertificateService.ICertificateService m_certificateService;

        public KestrelOptionsSetup(
            Microsoft.Extensions.Logging.ILogger<KestrelOptionsSetup> logger,
            Microsoft.Extensions.Options.IOptions<ProxyOptions> proxyOptions,
            libCertificateService.ICertificateService certificateService

        )
        {
            this.m_logger = logger;
            this.m_proxyOptions = proxyOptions.Value;
            this.m_certificateService = certificateService;
        } // End Constructor 


        ~KestrelOptionsSetup()
        { } // End Destructor 


        public static void ConfigureEndpointDefaults(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions)
        {
            Microsoft.Extensions.Logging.ILogger<Program> logger =
                listenOptions.ApplicationServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();

            // This works 
            Microsoft.AspNetCore.Connections.ConnectionBuilderExtensions.Use(listenOptions, async (connectionContext, next) =>
            {
                await ProxyProtocol.ProxyProtocol.ProcessAsync(connectionContext, next, logger);
            });

        } // End Sub ConfigureEndpointDefaults 


        public static System.Security.Cryptography.X509Certificates.X509Certificate2? 
            ServerCertificateSelector(
                System.Collections.Concurrent.ConcurrentDictionary<string, LetsEncryptData> certs,
                Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, 
                string name
        )
        {
            if (certs != null && certs.Count > 0)
            {
                if (string.IsNullOrEmpty(name))
                {
                    System.Net.IPEndPoint? ipe = (System.Net.IPEndPoint?)connectionContext?.LocalEndPoint;
                    if (ipe == null)
                        name = "unknown";
                    else
                    {
                        if (ipe.Address.IsIPv4MappedToIPv6)
                            name = ipe.Address.MapToIPv4().ToString();
                        else
                            name = ipe.Address.ToString();
                    }
                }

                if (certs.ContainsKey(name))
                    return certs[name].Certificate;

                foreach (System.Collections.Generic.KeyValuePair<string, LetsEncryptData> kvp
                    in certs)
                {
                    string altname = kvp.Key;

                    // https://serverfault.com/questions/104160/wildcard-ssl-certificate-for-second-level-subdomain/946120
                    // According to the RFC 6125, only a single wildcard is allowed in the most left fragment.
                    // Valid:
                    //   - *.sub.domain.tld
                    //   - *.domain.tld

                    // Invalid:
                    // sub.*.domain.tld
                    // *.*.domain.tld
                    // domain.*
                    // *.tld
                    // f*.com
                    // sub.*.*

                    // Also, note that 
                    // *.domain.com does not cover domain.com

                    if (altname.StartsWith("*"))
                    {
                        altname = altname.Substring(1); // .foo.int from *.foo.int 
                        if (name.EndsWith(altname, System.StringComparison.InvariantCultureIgnoreCase))
                            return kvp.Value.Certificate;

                        altname = altname.Substring(1); // foo.int from *.foo.int 
                        if (altname.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
                            return kvp.Value.Certificate;
                    }
                }

                // throw new System.IO.InvalidDataException("No certificate for name \"" + name + "\".");
                return null;
            } // End if (certs != null && certs.Count > 0) 

            throw new System.IO.InvalidDataException("No certificate for name \"" + name + "\".");
        } // End Function ServerCertificateSelector 



        public System.Security.Cryptography.X509Certificates.X509Certificate2 CertificateSelector(
            Microsoft.AspNetCore.Connections.ConnectionContext? connectionContext,
            string? name
        )
        {

            if (this.m_certificateService != null )
            {
                if (string.IsNullOrEmpty(name))
                {
                    System.Net.IPEndPoint? ipe = (System.Net.IPEndPoint?)connectionContext?.LocalEndPoint;
                    if (ipe == null)
                        name = "unknown";
                    else
                    {
                        if (ipe.Address.IsIPv4MappedToIPv6)
                            name = ipe.Address.MapToIPv4().ToString();
                        else
                            name = ipe.Address.ToString();
                    }
                } // End if (string.IsNullOrEmpty(name)) 

                System.Security.Cryptography.X509Certificates.X509Certificate2 cert = this.m_certificateService.GetCertificate2(name);
                if(cert != null)
                    return cert;
#if false

                foreach (System.Collections.Generic.KeyValuePair<string, LetsEncryptData> kvp
                    in certs)
                {
                    string altname = kvp.Key;

                    // https://serverfault.com/questions/104160/wildcard-ssl-certificate-for-second-level-subdomain/946120
                    // According to the RFC 6125, only a single wildcard is allowed in the most left fragment.
                    // Valid:
                    //   - *.sub.domain.tld
                    //   - *.domain.tld

                    // Invalid:
                    // sub.*.domain.tld
                    // *.*.domain.tld
                    // domain.*
                    // *.tld
                    // f*.com
                    // sub.*.*

                    // Also, note that 
                    // *.domain.com does not cover domain.com

                    if (altname.StartsWith("*"))
                    {
                        altname = altname.Substring(1); // .foo.int from *.foo.int 
                        if (name.EndsWith(altname, System.StringComparison.InvariantCultureIgnoreCase))
                            return kvp.Value.Certificate;

                        altname = altname.Substring(1); // foo.int from *.foo.int 
                        if (altname.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
                            return kvp.Value.Certificate;
                    }
#endif
            } // End if (this.m_certificateService != null ) 

            throw new System.IO.InvalidDataException("No certificate for name \"" + name + "\".");
        } // End Function CertificateSelector 


        private void SetListenOptions(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions listenOptions)
        {
            listenOptions.ServerCertificateSelector = this.CertificateSelector;
        } // End Sub SetListenOptions 


        void Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>.Configure(
                Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions kestrelOptions)
        {
            kestrelOptions.AddServerHeader = false;
            kestrelOptions.AllowSynchronousIO = false;

            kestrelOptions.Limits.MaxConcurrentConnections = 100;
            kestrelOptions.Limits.MaxConcurrentUpgradedConnections = 100;
            kestrelOptions.Limits.MaxRequestBodySize = 52428800;


            // with HAproxy, it must be here ? 
            // has nothing todo with HAproxy. 
            // I used HAproxy in tls-passthrough mode, and nginx in tls-terminating mode. 
            // in passthrough, the reverse-proxy does not have the certificate.
            // So he prepends the proxy-v2-header, and as payload the encrypted tls.
            // That means kestrel needs to process&skip/remove the proxy-header before it processes tls.
            // In non-passthrough mode, the tls-connection is terminated in the reverse-proxy,
            // the proxy-header is applied to the content, and that entire packet is tls-encrypted for kestrel.
            // kestrel receives, does tls-termination, and then needs to process&skip/remove
            // the Proxy-header only after decryption

            if (this.m_proxyOptions.SslPassthrough) // with TLS passthrough 
                kestrelOptions.ConfigureEndpointDefaults(ConfigureEndpointDefaults);

            kestrelOptions.ConfigureHttpsDefaults(this.SetListenOptions);

            // in NGINX, with SSL-termination - it must be HERE:
            if (!this.m_proxyOptions.SslPassthrough) // with TLS termination 
                kestrelOptions.ConfigureEndpointDefaults(ConfigureEndpointDefaults);
        } // End Sub Configure 


    } // End Class KestrelOptionsSetup 


} // End Namespace TestApplicationHttps 
