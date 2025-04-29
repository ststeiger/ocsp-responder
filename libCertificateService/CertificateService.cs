
namespace libCertificateService
{


    public class CertificateService 
        : ICertificateService
    {
        private System.DateTime m_lastRefreshTime;
        private readonly object m_refreshLock;
        private readonly Microsoft.Extensions.Logging.ILogger<CertificateService> m_logger;
        private readonly ICertificateRepository m_repository;

        // using ConcurrentDictionary to avoid locks while ensuring thread safety
        private System.Collections.Concurrent.ConcurrentDictionary<
                string, System.Security.Cryptography.X509Certificates.X509Certificate2
            > m_certificateMap;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<
            string, System.Security.Cryptography.X509Certificates.X509Certificate2
            > m_selfSignedCertificates;


        
        public CertificateService(
            Microsoft.Extensions.Logging.ILogger<CertificateService> logger, 
            ICertificateRepository repository
        )
        {
            this.m_lastRefreshTime = System.DateTime.MinValue;
            this.m_refreshLock = new object();

            this.m_logger = logger;
            this.m_repository = repository;

            this.m_certificateMap = new System.Collections.Concurrent.ConcurrentDictionary<
                string, System.Security.Cryptography.X509Certificates.X509Certificate2
                >(System.StringComparer.OrdinalIgnoreCase);
            
            this.m_selfSignedCertificates = new System.Collections.Concurrent.ConcurrentDictionary<
                string, System.Security.Cryptography.X509Certificates.X509Certificate2>(System.StringComparer.OrdinalIgnoreCase);

            // Initialize self-signed certificates (these are fixed and won't be updated from DB)
            InitializeSelfSignedCertificates();
        } // End Constructor 


        private void InitializeSelfSignedCertificates()
        {
            // Add self-signed certificates for localhost, NetBIOS name, and IP
            // These would be created or loaded from somewhere in a real implementation
            Certificate localhost = new Certificate
            {
                DomainName = "localhost",
                PfxData = new byte[0], // This would be actual certificate data
                ValidFrom = System.DateTime.UtcNow.AddYears(-1),
                ValidUntil = System.DateTime.UtcNow.AddYears(10),
                CreatedAt = System.DateTime.UtcNow
            };

            // this.m_selfSignedCertificates.TryAdd("localhost", localhost);

            // Add other self-signed certificates as needed
            // Example for local IP
            // this.m_selfSignedCertificates.TryAdd("127.0.0.1", localhost);
        } // End Sub InitializeSelfSignedCertificates 


        /// <summary>
        /// Refreshes certificates, if at least 1 hour passed sincle last refresh
        /// </summary>
        /// <param name="force">if true, will refresh certificate without checking elapsed time</param>
        public void RefreshCertificates(bool force)
        {
            try
            {
                if (!force)
                {
                    
                    lock (this.m_refreshLock)
                    {
                        if ((System.DateTime.UtcNow - this.m_lastRefreshTime).TotalHours < 1)
                            return;

                        this.m_lastRefreshTime = System.DateTime.UtcNow;
                    } // End Lock 

                } // End if (!force) 

                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(this.m_logger, "Starting certificate refresh");

                // The 'latestCertificates' dictionary now contains the latest certificate for each domain
                System.Collections.Generic.List<Certificate> allCertificates = this.m_repository.GetAllValidCertificates();

                // Group by domain and take the latest certificate for each domain
                System.Collections.Generic.Dictionary<string, System.Security.Cryptography.X509Certificates.X509Certificate2> latestCertificates =
                    new System.Collections.Generic.Dictionary<
                        string, System.Security.Cryptography.X509Certificates.X509Certificate2
                        >(System.StringComparer.OrdinalIgnoreCase);

                foreach (Certificate certificate in allCertificates)
                {
                    string domainName = certificate.DomainName;
                    System.Security.Cryptography.X509Certificates.X509Certificate2 tlsCertificate =
                        new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.PfxData);

                    latestCertificates.Add(domainName, tlsCertificate);
                } // Next certificate 

                // Create a new dictionary with the latest certificates
                System.Collections.Concurrent.ConcurrentDictionary<string, System.Security.Cryptography.X509Certificates.X509Certificate2> newCertificatesMap =
                    new System.Collections.Concurrent.ConcurrentDictionary<
                        string, System.Security.Cryptography.X509Certificates.X509Certificate2
                        >(latestCertificates, System.StringComparer.OrdinalIgnoreCase);

                // Replace the old map with the new one (atomic operation)
                // This ensures GetCertificate operation is never interrupted
                System.Threading.Interlocked.Exchange(ref this.m_certificateMap, newCertificatesMap);

                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(this.m_logger, $"Certificate refresh completed. Total certificates: {newCertificatesMap.Count}");
            } // End Try 
            catch (System.Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(this.m_logger, ex, "Error refreshing certificates");
                throw; // Re-throw the exception to be handled by calling code
            } // End Catch 

        } // End Sub RefreshCertificates 


        private System.Security.Cryptography.X509Certificates.X509Certificate2? GetCertificate(string domainName, bool retry)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2? retValue = null;

            // First check self-signed certificates
            if (this.m_selfSignedCertificates.TryGetValue(domainName, out retValue))
            {
                return retValue;
            } // End if (this.m_selfSignedCertificates.TryGetValue(domainName, out retValue)) 

            // Then check internet domain certificates
            if (this.m_certificateMap.TryGetValue(domainName, out retValue))
            {
                return retValue;
            } // End if (this.m_certificateMap.TryGetValue(domainName, out retValue)) 

            if (!retry)
            {
                // If not found in memory, try to get from repository and update our cache
                RefreshCertificates(false);
                return this.GetCertificate(domainName, true);
            } // End if (!retry) 

            return null;
        } // End Function GetCertificate 

        System.Security.Cryptography.X509Certificates.X509Certificate2? ICertificateService.GetCertificate(string domainName)
        {
            return this.GetCertificate(domainName, false);
        } // End Task GetCertificate 


    } // End Class CertificateService 


} // End Namespace 
