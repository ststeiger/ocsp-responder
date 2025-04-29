
namespace OCSPGuardian
{


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
            this.m_logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            this.m_proxyOptions = proxyOptions?.Value ?? throw new System.ArgumentNullException(nameof(proxyOptions));
            this.m_certificateService = certificateService ?? throw new System.ArgumentNullException(nameof(certificateService));
        } // End Constructor 


        ~KestrelOptionsSetup()
        { } // End Destructor 


        public void ConfigureEndpointDefaults(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions)
        {
            // This works 
            Microsoft.AspNetCore.Connections.ConnectionBuilderExtensions.Use(listenOptions, async (connectionContext, next) =>
            {
                await ProxyProtocol.ProxyProtocol.ProcessAsync(connectionContext, next, this.m_logger);
            });

        } // End Sub ConfigureEndpointDefaults 


        private string GetDomainNameFromSni(
            Microsoft.AspNetCore.Connections.ConnectionContext? connectionContext,
            string? name
        )
        {
            if (!string.IsNullOrEmpty(name))
                return name;

            if (connectionContext == null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogCritical(this.m_logger, "Warning: Connection context is null");
                return "UNKNOWN";
            } // End if (connectionContext == null) 


            if (connectionContext.LocalEndPoint != null)
            {

                switch (connectionContext.LocalEndPoint)
                {
                    case System.Net.IPEndPoint ipe:
                        name = ipe.Address.IsIPv4MappedToIPv6
                            ? ipe.Address.MapToIPv4().ToString()
                            : ipe.Address.ToString();
                        break;
                    case System.Net.Sockets.UnixDomainSocketEndPoint uds:
                        name = uds.ToString();
                        // Extract domain from the socket path (e.g., "/path/to/example.com.sock" → "example.com") 
                        // If your Unix domain sockets follow a pattern like /path/to/example.com.sock, 
                        // you might want to extract just example.com: 
                        name = System.IO.Path.GetFileNameWithoutExtension(name);
                        break;
                } // End Switch 

            } // End if (connectionContext.LocalEndPoint != null) 


            // Fallback to SNI if we still don't have a name
            if(name == null)
                name = connectionContext.Features.Get<Microsoft.AspNetCore.Connections.Features.ITlsHandshakeFeature>()?.HostName;

            if (string.IsNullOrEmpty(name))
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogCritical(
                    this.m_logger,
                    $"Warning: Could not determine certificate name for connection: {connectionContext.ConnectionId}"
                );
                name = "UNKNOWN";
            } // End if (string.IsNullOrEmpty(name)) 

            return name;
        } // End Function GetDomainNameFromSni 


        public System.Security.Cryptography.X509Certificates.X509Certificate2 CertificateSelector(
            Microsoft.AspNetCore.Connections.ConnectionContext? connectionContext,
            string? sniName
        )
        {
            if (this.m_certificateService == null)
                throw new System.InvalidProgramException("Certificate service not available");


            string domainName = GetDomainNameFromSni(connectionContext, sniName);

            System.Security.Cryptography.X509Certificates.X509Certificate2 cert = this.m_certificateService.GetCertificate2(domainName);
            if (cert != null)
                return cert;
#if false

            foreach (System.Collections.Generic.KeyValuePair<string, LetsEncryptData> kvp in certs)
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
                    if (domainName.EndsWith(altname, System.StringComparison.InvariantCultureIgnoreCase))
                        return kvp.Value.Certificate;

                    altname = altname.Substring(1); // foo.int from *.foo.int 
                    if (altname.Equals(domainName, System.StringComparison.InvariantCultureIgnoreCase))
                        return kvp.Value.Certificate;
                }
#endif

            throw new System.IO.InvalidDataException("No certificate for name \"" + domainName + "\".");
        } // End Function CertificateSelector 


        private void ConfigureHttpsDefaults(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions listenOptions)
        {
            listenOptions.ServerCertificateSelector = this.CertificateSelector;
        } // End Sub ConfigureHttpsDefaults 


        /// <summary>
        /// Invoked to configure a <typeparamref name="KestrelServerOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to configure.</param>
        void Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
            .Configure(
                Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions kestrelOptions)
        {
            kestrelOptions.AddServerHeader = false;
            kestrelOptions.AllowSynchronousIO = false;

            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                Microsoft.AspNetCore.Hosting.KestrelServerOptionsSystemdExtensions.UseSystemd(kestrelOptions);

            kestrelOptions.Limits.MaxConcurrentConnections = 100;
            kestrelOptions.Limits.MaxConcurrentUpgradedConnections = 100;
            kestrelOptions.Limits.MaxRequestBodySize = 52428800;


            // with HAproxy, it must be here ? 
            if (this.m_proxyOptions.SslPassthrough)
                kestrelOptions.ConfigureEndpointDefaults(ConfigureEndpointDefaults);

            kestrelOptions.ConfigureHttpsDefaults(this.ConfigureHttpsDefaults);

            // in NGINX, with SSL-termination - it must be HERE:
            if (!this.m_proxyOptions.SslPassthrough)
                kestrelOptions.ConfigureEndpointDefaults(ConfigureEndpointDefaults);
        } // End Sub Configure 


    } // End Class KestrelOptionsSetup 



} // End Namespace TestApplicationHttps 
