
namespace libCertificateService
{

    using Dapper;



    public class PostgresCertificateRepository 
        : ICertificateRepository
    {
        private readonly IDbConnectionFactory m_connectionFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<PostgresCertificateRepository> m_logger;


        public PostgresCertificateRepository(
            IDbConnectionFactory connectionFactory,
            Microsoft.Extensions.Logging.ILogger<PostgresCertificateRepository> logger)
        {
            this.m_connectionFactory = connectionFactory ?? throw new System.ArgumentNullException(nameof(connectionFactory));
            this.m_logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        } // End Constructor 


        public System.Collections.Generic.List<Certificate> GetAllValidCertificates()
        {
            try
            {
                const string sql = @"
SELECT 
	 t.cert_id 
	,t.domain_punycode  
	,t.cert_certificate_pfx_data 
	,t.cert_created_at 
	,t.ct_name 
	,t.cstat_name 
FROM 
(
	SELECT 
		 certificate.cert_id 
		,domain.domain_punycode 
	 
		,certificate.cert_pfx_data 
		,certificate.cert_created_at 
		,certificate_type.ct_name 
		,certificate_status.cstat_name 
		,ROW_NUMBER() OVER(PARTITION BY domain.domain_punycode ORDER BY certificate.cert_created_at DESC) AS cert_order 
	FROM ssl_certificates3.tls_certificates.certificate 

	INNER JOIN ssl_certificates3.tls_certificates.domain 
		ON domain.domain_id = certificate.cert_domain_id 

	INNER JOIN ssl_certificates3.tls_certificates.certificate_type 
		ON ct_id = certificate.cert_ct_id 
  
	INNER JOIN ssl_certificates3.tls_certificates.certificate_status 
		ON certificate_status.cstat_id = certificate.cert_cstat_id 

	WHERE CURRENT_TIMESTAMP BETWEEN certificate.cert_valid_from AND certificate.cert_valid_until 
    AND certificate.cert_ct_id = 1 -- Single Certificate 
	-- AND certificate.cert_ct_id = 2 -- Wildcard Certificate 
) AS t 
WHERE t.cert_order = 1 
";

                System.Collections.Generic.List<Certificate> lsCertificates = null;

                using (System.Data.Common.DbConnection connection = this.m_connectionFactory.Connection)
                {
                    System.Collections.Generic.IEnumerable<Certificate> certificates = 
                        connection.Query<Certificate>(sql);

                    lsCertificates = certificates.AsList();
                }

                return lsCertificates;
            }
            catch (System.Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(
                    this.m_logger, 
                    ex, 
                    "Error retrieving valid certificates from the database"
                );

                throw;
            }
        } // End Task GetAllValidCertificates 


        public async System.Threading.Tasks.Task<Certificate> GetLatestValidCertificateForDomain(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new System.ArgumentException("Domain name cannot be null or empty", nameof(domainName));
            }

            try
            {
                const string sql = @"
                    SELECT 
                        cert_domain_name AS DomainName,
                        cert_pfx_data AS PfxData,
                        cert_valid_from AS ValidFrom,
                        cert_valid_until AS ValidUntil, 
                        cert_created_at AS CreatedAt
                    FROM certificates 
                    WHERE cert_domain_name = @DomainName
                    AND cert_valid_from <= NOW() 
                    AND cert_valid_until > NOW() 
                    ORDER BY cert_created_at DESC 
                    LIMIT 1";

                Certificate certificate = null;

                using (System.Data.Common.DbConnection connection = this.m_connectionFactory.Connection)
                {
                    certificate = await connection.QueryFirstOrDefaultAsync<Certificate>(
                        sql,
                        new { DomainName = domainName }
                    );
                } // End Using connection 

                return certificate;
            } // End Try 
            catch (System.Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(
                    this.m_logger, 
                    ex, 
                    "Error retrieving latest valid certificate for domain {DomainName}", 
                    domainName
                );

                throw;
            } // End Catch 

        } // End Task GetLatestValidCertificateForDomain 


    } // End Class PostgresCertificateRepository 


} // End Namespace 
