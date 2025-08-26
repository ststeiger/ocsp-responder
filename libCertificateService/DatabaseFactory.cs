
namespace libCertificateService
{

    using Microsoft.Extensions.Configuration;
    

    public class ConnectionConfig
    {
        public string? Key { get; set; }
        public string? ConnectionString { get; set; }
        public string? ProviderName { get; set; }
    } // End Class ConnectionConfig 


    public class DatabaseFactory
        : System.IDisposable
    {
        private System.Collections.Generic.Dictionary<string, ConnectionConfig?>? m_allConnections;
        private ConnectionConfig? m_defaultConnection;
        private System.Data.Common.DbProviderFactory? m_providerFactory;
        private readonly Microsoft.Extensions.Configuration.IConfiguration? m_configuration;
        private System.IDisposable? m_changeTokenRegistration;


        public DatabaseFactory(DatabaseOptions options)
        {  }


        public DatabaseFactory(
            Microsoft.Extensions.Options.IOptions<DatabaseOptions> options)
            :this(options.Value)
        { }

        public DatabaseFactory(
            Microsoft.Extensions.Configuration.IConfiguration configuration
        )
        {
            this.m_configuration = configuration;
            LoadConnectionConfigurations();

            // Register for configuration changes
            SubscribeToConfigChanges();
        }


        // Method to be called when the configuration changes
        private void OnConfigurationChanged(object? sender)
        {
            ReloadConnectionConfigurations();

            // Re-subscribe for future changes
            SubscribeToConfigChanges();
        }


        private void SubscribeToConfigChanges()
        {
            // Get a change token from the configuration
            Microsoft.Extensions.Primitives.IChangeToken changeToken = this.m_configuration.GetReloadToken();

            // Register a callback that will be invoked when the configuration changes
            this.m_changeTokenRegistration = changeToken.RegisterChangeCallback(
                this.OnConfigurationChanged,
                null
            );

        } // End Sub SubscribeToConfigChanges 


        public void ReloadConnectionConfigurations()
        {
            System.Console.WriteLine("Configuration change detected. Reloading connection configurations...");

            try
            {
                LoadConnectionConfigurations();
                System.Console.WriteLine("Connection configurations successfully reloaded.");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error reloading connection configurations: {ex.Message}");
                // Log the error but don't throw - this allows the application to continue with existing connections
            }
        } // End Sub ReloadConnectionConfigurations 


        private void LoadConnectionConfigurations()
        {
            Microsoft.Extensions.Configuration.IConfigurationSection configSection = this.m_configuration.GetSection("TypedConnectionStrings");
            if (!configSection.Exists())
                throw new System.Exception("No section TypedConnectionStrings in appsettings.json");

            // Create a new dictionary to avoid disrupting any ongoing operations
            System.Collections.Generic.Dictionary<string, ConnectionConfig?> newConnections =
                new System.Collections.Generic.Dictionary<string, ConnectionConfig?>(System.StringComparer.InvariantCultureIgnoreCase);

            foreach (Microsoft.Extensions.Configuration.IConfigurationSection 
                thisSection in configSection.GetChildren()
            )
            {
                ConnectionConfig? thisConfig = thisSection.Get<ConnectionConfig>();
                if (thisConfig != null)
                {
                    thisConfig.Key = thisSection.Key;
                    newConnections.Add(thisSection.Key, thisConfig);
                }
            } // Next thisSection 

            // Get machine name
            string machineName = System.Environment.MachineName;

            // Try to get config for this machine
            ConnectionConfig? newConfig = null;
            if (newConnections.ContainsKey(machineName))
                newConfig = newConnections[machineName];

            // If not found, try server as fallback
            if (newConfig == null || string.IsNullOrEmpty(newConfig.ConnectionString))
            {
                if (newConnections.ContainsKey("server"))
                    newConfig = newConnections["server"];
            }

            if (newConfig == null || string.IsNullOrEmpty(newConfig.ConnectionString) || string.IsNullOrEmpty(newConfig.ProviderName))
                throw new System.InvalidOperationException("No valid connection configuration found or missing provider information.");

            // Atomic updates to class state
            string? oldProviderName = this.m_defaultConnection?.ProviderName;

            this.m_allConnections = newConnections;
            this.m_defaultConnection = newConfig;

            // Only create a new provider factory if the provider name changed
            if (oldProviderName == null || !oldProviderName.Equals(newConfig.ProviderName, System.StringComparison.OrdinalIgnoreCase))
            {
                this.m_providerFactory = CreateProviderFactory(
                    this.m_defaultConnection?.ProviderName
                );
            }
        } // End Sub LoadConnectionConfigurations 


        /// <summary>
        /// Creates the DbProviderFactory based on provider type 
        /// </summary>
        private System.Data.Common.DbProviderFactory CreateProviderFactory(
            System.Type? providerFactoryType
        )
        {
            if (providerFactoryType == null)
                throw new System.ArgumentNullException(nameof(providerFactoryType));

            try
            {
                // Check if it inherits from DbProviderFactory
                if (!typeof(System.Data.Common.DbProviderFactory).IsAssignableFrom(providerFactoryType))
                    throw new System.InvalidOperationException($"Provider type '{providerFactoryType.FullName}' is not a System.Data.Common.DbProviderFactory.");

                // Get the Instance property via reflection (all DbProviderFactory classes should have a static Instance property/field)
                System.Data.Common.DbProviderFactory? instance = null;

                // Try to get the Instance field (since Npgsql uses a field)
                System.Reflection.FieldInfo? instanceField = providerFactoryType.GetField("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                // Get the factory instance
                if (instanceField != null)
                    instance = (System.Data.Common.DbProviderFactory?)instanceField.GetValue(null);

                if (instance == null)
                {
                    System.Reflection.PropertyInfo? instanceProperty = providerFactoryType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceProperty != null)
                        // Get the factory instance
                        instance = (System.Data.Common.DbProviderFactory?)instanceProperty.GetValue(null);
                }

                if (instance == null)
                    throw new System.InvalidOperationException($"Provider type '{providerFactoryType.FullName}' does not have an Instance field/property.");

                return instance;
            }
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException($"Failed to create provider factory for '{providerFactoryType.FullName}': {ex.Message}", ex);
            }
        } // End Function CreateProviderFactory 


        /// <summary>
        /// Creates the DbProviderFactory based on provider name
        /// </summary>
        private System.Data.Common.DbProviderFactory CreateProviderFactory(
            string? providerName
        )
        {
            if (providerName == null)
                throw new System.ArgumentNullException(nameof(providerName));

            // Try to load the provider factory using the assembly-qualified name
            System.Type? providerFactoryType = System.Type.GetType(providerName);
            return CreateProviderFactory(providerFactoryType);
        } // End Function CreateProviderFactory 


        /// <summary>
        /// Creates a database connection based on the provider
        /// </summary>
        public System.Data.Common.DbConnection CreateConnection(bool open)
        {
            System.Data.Common.DbConnection? connection = this.m_providerFactory!.CreateConnection();
            connection.ConnectionString = this.m_defaultConnection!.ConnectionString;

            if (open)
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    connection.Open();
            }

            return connection;
        } // End Function CreateConnection 


        /// <summary>
        /// Creates a database connection based on the provider
        /// </summary>
        public System.Data.Common.DbConnection Connection
        {
            get
            {
                System.Data.Common.DbConnection? connection = this.m_providerFactory!.CreateConnection();
                connection.ConnectionString = this.m_defaultConnection!.ConnectionString;

                return connection;
            }
        } // End Property Connection 


        /// <summary>
        /// Gets the current provider name
        /// </summary>
        public string ProviderName
        {
            get
            {
                return this.m_defaultConnection!.ProviderName!;
            }
        }


        /// <summary>
        /// Gets the connection string being used
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return this.m_defaultConnection!.ConnectionString!;
            }
        }

        /// <summary>
        /// Helper method to check if current provider is PostgreSQL
        /// </summary>
        public bool IsPostgreSQL
        {
            get
            {
                return this.m_defaultConnection!.ProviderName!.IndexOf("Npgsql", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }


        /// <summary>
        /// Helper method to check if current provider is SQL Server
        /// </summary>
        public bool IsSqlServer
        {
            get
            {
                return this.m_defaultConnection!.ProviderName!.IndexOf("SqlClient", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }


        /// <summary>
        /// Helper method to check if current provider is MySQL
        /// </summary>
        public bool IsMySQL
        {
            get
            {
                return this.m_defaultConnection!.ProviderName!.IndexOf("MySql", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// Helper method to check if current provider is Oracle
        /// </summary>
        public bool IsOracle
        {
            get
            {
                return this.m_defaultConnection!.ProviderName!.IndexOf("Oracle", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }


        /// <summary>
        /// Helper method to format identifier (table/column names) according to provider syntax
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            if (IsSqlServer)
                return $"[{identifier}]";
            else if (IsPostgreSQL || IsOracle)
                return $"\"{identifier}\"";
            else if (IsMySQL)
                return $"`{identifier}`";
            else
                return identifier;
        } // End Sub Dispose 


        public void Dispose()
        {
            this.m_changeTokenRegistration?.Dispose();
        } // End Sub Dispose 


    } // End Class DatabaseFactory 


} // End Namespace 
