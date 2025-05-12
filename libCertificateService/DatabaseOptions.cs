
namespace libCertificateService
{

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    

    public class DatabaseOptions
    {
        public string? ConnectionString { get; set; }
        public string? Provider { get; set; }

        public System.Data.Common.DbProviderFactory? ProviderFactory { get; set; }
    }

    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Creates the DbProviderFactory based on provider type 
        /// </summary>
        private static System.Data.Common.DbProviderFactory CreateProviderFactory(
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
            } // End Try 
            catch (System.Exception ex)
            {
                throw new System.InvalidOperationException($"Failed to create provider factory for '{providerFactoryType.FullName}': {ex.Message}", ex);
            } // End Catch 
        } // End Function CreateProviderFactory 


        /// <summary>
        /// Creates the DbProviderFactory based on provider name
        /// </summary>
        private static System.Data.Common.DbProviderFactory CreateProviderFactory(
            string providerName
        )
        {
            // Try to load the provider factory using the assembly-qualified name
            System.Type? providerFactoryType = System.Type.GetType(providerName);
            return CreateProviderFactory(providerFactoryType);
        } // End Function CreateProviderFactory 


        public static IServiceCollection AddDatabaseFactoryServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // requires package Microsoft.Extensions.Options.ConfigurationExtensions 
            //services.Configure<DatabaseOptions>(
            //    configuration.GetSection("Database")
            //);

            services.Configure<DatabaseOptions>(options =>
            {
                // Bind all values from configuration first
                configuration.GetSection("Database").Bind(options);

                // Then set additional values that require manual logic
                if(!string.IsNullOrEmpty(options.Provider))
                    options.ProviderFactory = CreateProviderFactory(options.Provider!);
            });

            // Register PostgresConnectionFactory as a singleton
            // services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

            return services;
        } // End Function AddPostgresServices 


    } // End Class ServiceCollectionExtensions 


} // End Namespace 
