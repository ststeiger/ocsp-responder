
namespace libCertificateService
{


    public class DatabaseOptions
    {
        public string ConnectionString { get; set; }
    }


    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection Connection { get; }
    }

    public class PostgresConnectionFactory : IDbConnectionFactory, System.IDisposable
    {
        private readonly string _connectionString;
        private System.Data.Common.DbConnection _connection;
        private bool _disposed;

        public PostgresConnectionFactory(Microsoft.Extensions.Options.IOptions<DatabaseOptions> options)
        {
            _connectionString = options.Value.ConnectionString ??
                throw new System.ArgumentNullException(nameof(options.Value.ConnectionString));
        }

        public System.Data.Common.DbConnection Connection
        {
            get
            {
                // Create a new connection if one doesn't exist or if it's closed
                if (_connection == null || _connection.State == System.Data.ConnectionState.Closed)
                {
                    // _connection = new NpgsqlConnection(_connectionString);
                }
                return _connection;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                }

                _disposed = true;
            }
        }
    }


}
