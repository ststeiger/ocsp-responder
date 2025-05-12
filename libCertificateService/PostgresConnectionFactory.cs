
namespace libCertificateService
{


    public class PostgresConnectionFactory 
        : IDbConnectionFactory
    {
        private string m_connectionString;
        private readonly System.IDisposable? m_optionsChangeListener;
        private readonly Microsoft.Extensions.Options.IOptionsMonitor<
            DatabaseOptions
            >? m_optionsMonitor;


        private void UpdateConnectionString(DatabaseOptions options, string? s)
        {
            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new System.ArgumentNullException(
                    nameof(options.ConnectionString)
                );

            this.m_connectionString = options.ConnectionString!;

            // Optionally, you could invalidate any existing connections here
            // or maintain a connection pool that gets refreshed
        } // End Sub UpdateConnectionString 


        public PostgresConnectionFactory(
            Microsoft.Extensions.Options.IOptions<DatabaseOptions> options
        )
        {
            this.m_connectionString = options.Value.ConnectionString ??
                throw new System.ArgumentNullException(
                    nameof(options.Value.ConnectionString)
            );
        } // End Constructor 


        public PostgresConnectionFactory(
            Microsoft.Extensions.Options.IOptionsMonitor<DatabaseOptions> optionsMonitor
        )
        {
            this.m_optionsMonitor = optionsMonitor ?? 
                throw new System.ArgumentNullException(nameof(optionsMonitor));

            
            this.m_connectionString = optionsMonitor.CurrentValue.ConnectionString ??
                throw new System.ArgumentNullException(
                    nameof(optionsMonitor.CurrentValue.ConnectionString)
                );

            // Register for changes to options
            this.m_optionsChangeListener = this.m_optionsMonitor
                .OnChange(this.UpdateConnectionString);
        } // End Constructor 


        public System.Data.Common.DbConnection Connection
        {
            get
            {
                // Create a new connection if one doesn't exist or if it's closed
                System.Data.Common.DbConnection connection = null!; // new NpgsqlConnection(this.m_connectionString);
                connection.ConnectionString = this.m_connectionString;

                return connection;
            }
        } // End Property Connection 


    } // End Class PostgresConnectionFactory 


} // End Namespace 
