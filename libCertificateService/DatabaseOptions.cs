
namespace libCertificateService
{

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    

    public class DatabaseOptions
    {
        public string? ConnectionString { get; set; }

        // public string? Provider { get; set; }
    }

    public static class ServiceCollectionExtensions
    {
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
                // options.DbProviderFactory = System.Data.SqlClient.SqlClientFactory.Instance;
            });

            // Register PostgresConnectionFactory as a singleton
            // services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();

            return services;
        } // End Function AddPostgresServices 


    } // End Class ServiceCollectionExtensions 


}

