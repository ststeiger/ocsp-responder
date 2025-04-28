
namespace libCertificateService
{

    using Dapper;
    

    public class PostgresCertificateRepository 
        : ICertificateRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<PostgresCertificateRepository> _logger;

        public PostgresCertificateRepository(
            IDbConnectionFactory connectionFactory,
            Microsoft.Extensions.Logging.ILogger<PostgresCertificateRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new System.ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        } // End Constructor 


        public async System.Threading.Tasks.Task<System.Collections.Generic.List<Certificate>> GetAllValidCertificates()
        {
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
                    WHERE cert_valid_until > NOW()";

                System.Collections.Generic.List<Certificate> lsCertificates = null;

                using (System.Data.Common.DbConnection connection = _connectionFactory.Connection)
                {
                    System.Collections.Generic.IEnumerable<Certificate> certificates = 
                        await connection.QueryAsync<Certificate>(sql);

                    lsCertificates = certificates.AsList();
                }

                return lsCertificates;
            }
            catch (System.Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "Error retrieving valid certificates from the database");
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

                using (System.Data.Common.DbConnection connection = _connectionFactory.Connection)
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
                    _logger, 
                    ex, 
                    "Error retrieving latest valid certificate for domain {DomainName}", 
                    domainName
                );

                throw;
            } // End Catch 

        } // End Task GetLatestValidCertificateForDomain 


    } // End Class PostgresCertificateRepository 


} // End Namespace 
