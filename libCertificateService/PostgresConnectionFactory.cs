
namespace libCertificateService
{


    public class DatabaseOptions
    {
        public string? ConnectionString { get; set; }
    }


    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection Connection { get; }
    }


    public class PostgresConnectionFactory 
        : IDbConnectionFactory
    {
        private readonly string m_connectionString;
        private bool _disposed;

        public PostgresConnectionFactory(Microsoft.Extensions.Options.IOptions<DatabaseOptions> options)
        {
            this.m_connectionString = options.Value.ConnectionString ??
                throw new System.ArgumentNullException(nameof(options.Value.ConnectionString));
        }

        public System.Data.Common.DbConnection Connection
        {
            get
            {
                // Create a new connection if one doesn't exist or if it's closed
                System.Data.Common.DbConnection connection = null!; // new NpgsqlConnection(this.m_connectionString);
                return connection;
            }
        }


    }


}
