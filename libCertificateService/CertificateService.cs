
namespace libCertificateService
{


    public class CertificateService 
        : ICertificateService
    {
        private readonly Microsoft.Extensions.Logging.ILogger<CertificateService> m_logger;
        private readonly ICertificateRepository m_repository;

        // Using ConcurrentDictionary to avoid locks while ensuring thread safety
        private System.Collections.Concurrent.ConcurrentDictionary<string, Certificate> m_certificateMap;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Certificate> m_selfSignedCertificates;


        public CertificateService(
            Microsoft.Extensions.Logging.ILogger<CertificateService> logger, 
            ICertificateRepository repository
        )
        {
            this.m_logger = logger;
            this.m_repository = repository;

            this.m_certificateMap = new System.Collections.Concurrent.ConcurrentDictionary<string, Certificate>(
                System.StringComparer.OrdinalIgnoreCase
            );
            
            this.m_selfSignedCertificates = new System.Collections.Concurrent.ConcurrentDictionary<string, Certificate>(
                System.StringComparer.OrdinalIgnoreCase
            );

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

            this.m_selfSignedCertificates.TryAdd("localhost", localhost);

            // Add other self-signed certificates as needed
            // Example for local IP
            this.m_selfSignedCertificates.TryAdd("127.0.0.1", localhost);
        } // End Sub InitializeSelfSignedCertificates 


        public async System.Threading.Tasks.Task<Certificate> GetCertificate(string domainName)
        {
            // First check self-signed certificates
            if (this.m_selfSignedCertificates.TryGetValue(domainName, out Certificate selfSignedCert))
            {
                return selfSignedCert;
            } // End if (this.m_selfSignedCertificates.TryGetValue(domainName, out Certificate selfSignedCert)) 

            // Then check internet domain certificates
            if (this.m_certificateMap.TryGetValue(domainName, out Certificate internetCert))
            {
                return internetCert;
            } // End if (this.m_certificateMap.TryGetValue(domainName, out Certificate internetCert)) 

            // If not found in memory, try to get from repository and update our cache
            Certificate certificate = await this.m_repository.GetLatestValidCertificateForDomain(domainName);
            if (certificate != null)
            {
                this.m_certificateMap.TryAdd(domainName, certificate);
                return certificate;
            } // End if (certificate != null) 

            return null;
        } // End Task GetCertificate 


        public async System.Threading.Tasks.Task RefreshCertificates()
        {
            try
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(this.m_logger, "Starting certificate refresh");

                // The 'latestCertificates' dictionary now contains the latest certificate for each domain
                System.Collections.Generic.List<Certificate> allCertificates = await this.m_repository.GetAllValidCertificates();

                // Group by domain and take the latest certificate for each domain
                System.Collections.Generic.Dictionary<string, Certificate> latestCertificates =
                    new System.Collections.Generic.Dictionary<string, Certificate>(System.StringComparer.OrdinalIgnoreCase);

                foreach (Certificate certificate in allCertificates)
                {
                    string domainName = certificate.DomainName;
                    latestCertificates.Add(domainName, certificate);
                } // Next certificate 

                // Create a new dictionary with the latest certificates
                System.Collections.Concurrent.ConcurrentDictionary<string, Certificate> newCertificatesMap =
                    new System.Collections.Concurrent.ConcurrentDictionary<string, Certificate>(
                    latestCertificates,
                    System.StringComparer.OrdinalIgnoreCase
                );

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

        } // End Task RefreshCertificates 


    } // End Class CertificateService 


} // End Namespace 
