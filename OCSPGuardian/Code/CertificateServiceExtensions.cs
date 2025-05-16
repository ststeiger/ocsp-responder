
namespace CertificateManagement
{
    using libCertificateService;
    using Microsoft.AspNetCore.Builder; // for MapGet 
    using Microsoft.Extensions.DependencyInjection; // for AddSingleton 
    using Quartz;


    public class CertificateRefreshJob
        : Quartz.IJob
    {
        private readonly ICertificateService _certificateService;
        private readonly Microsoft.Extensions.Logging.ILogger<CertificateRefreshJob> _logger;


        public CertificateRefreshJob(
            ICertificateService certificateService,
            Microsoft.Extensions.Logging.ILogger<CertificateRefreshJob> logger)
        {
            _certificateService = certificateService;
            _logger = logger;
        } // End Constructor 


        public async System.Threading.Tasks.Task Execute(Quartz.IJobExecutionContext context)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "Certificate refresh job started");
            await _certificateService.RefreshCertificates();
            Microsoft.Extensions.Logging.LoggerExtensions.LogInformation(_logger, "Certificate refresh job completed");
        } // End Task Execute 


    } // End Class CertificateRefreshJob 


    public static class CertificateServiceExtensions
    {


        private class RefreshHandler
        {


            public static async System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IResult>
                    RefreshCertificates(ICertificateService certificateService)
            {
                await certificateService.RefreshCertificates();
                return Microsoft.AspNetCore.Http.Results.Ok(new { message = "Certificate refresh triggered successfully" });
            } // End Task RefreshCertificates 

        } // End Class RefreshHandler 


        public static void AddCertificateService(
            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            string cronSchedule
        )
        {
            // Register services
            services.AddSingleton<ICertificateRepository, PostgresCertificateRepository>();
            services.AddSingleton<ICertificateService, CertificateService>();


            // Quartz.AspNetCore is suitable for traditional ASP.NET Core web applications,
            // while Quartz.Extensions.Hosting  is often preferred for worker services or applications using the generic host.
            // Both support integrating with the ASP.NET Core DI.

            // Register and configure Quartz.NET
            services.AddQuartz(q =>
            {
                // Create a "certificate-refresh-job" with the CertificateRefreshJob class
                Quartz.JobKey certificateRefreshJobKey = new Quartz.JobKey("certificate-refresh-job");

                q.AddJob<CertificateRefreshJob>(opts => opts.WithIdentity(certificateRefreshJobKey));

                // Create a trigger with the provided cron schedule
                q.AddTrigger(opts => opts
                    .ForJob(certificateRefreshJobKey)
                    .WithIdentity("certificate-refresh-trigger")
                    .WithCronSchedule(cronSchedule));
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        } // End Sub AddCertificateService 


        public static void AddCertificateService(
            this Microsoft.Extensions.DependencyInjection.IServiceCollection services
        )
        {
            AddCertificateService(services, "0 0 * * * ?");
        } // End Sub AddCertificateService 


        public static void MapCertificateEndpoints(
            this Microsoft.AspNetCore.Builder.WebApplication app,
            string pattern
        )
        {
            app.MapGet(pattern, RefreshHandler.RefreshCertificates)
            .WithName("RefreshCertificates");
        } // End Sub MapCertificateEndpoints 


    } // End Class CertificateServiceExtensions 



    // Example of Program.cs showing how to use the service
    public class NotProgram
    {


        public static void NotMain(string[] args)
        {
            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            // Add the certificate service with a daily refresh at midnight
            builder.Services.AddCertificateService("0 0 0 * * ?");

            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // Map the certificate refresh endpoint
            app.MapCertificateEndpoints("/api/certificates/refresh");

            app.Run();
        } // End Sub NotMain 


    } // End Class NotProgram 


} // End Namespace  
