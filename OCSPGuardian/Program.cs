
namespace OCSPGuardian
{

    using Microsoft.AspNetCore.Builder; // Use*, Map*
    using Microsoft.Extensions.DependencyInjection; // AddSingleton
    using Microsoft.Extensions.Hosting; // IsDevelopment
    

    public class Program
    {


        private static void AddStaticResponseHeaders(Microsoft.AspNetCore.StaticFiles.StaticFileResponseContext ctx)
        {
            // Set cache control headers
            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        } // End Sub AddStaticResponseHeaders 


        public static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            ApplicationConfiguration.Add(builder.Configuration, isWindows);


            HostConfigurator.Configure(builder, builder.Configuration, isWindows);
            ApplicationHttpsRedirectPolicy.ConfigureHttpsRedirection(builder.Services, builder.Configuration);
            builder.Services.AddSingleton(builder.Environment);

            



            // Add services to the container.
            // builder.Services.AddRazorPages();

            // Register IOcspLogger
            builder.Services.AddSingleton<global::OcspResponder.Core.IOcspLogger, SimpleOcspLogger>();
            builder.Services.AddSingleton<global::OcspResponder.Core.IOcspResponderRepository, RepoClass>();
            builder.Services.AddSingleton<global::OcspResponder.Core.IOcspResponder, global::OcspResponder.Core.OcspResponder>();

            


            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            } // End if (!app.Environment.IsDevelopment()) 

            app.UseHttpsRedirection();



            Microsoft.AspNetCore.Builder.DefaultFilesOptions options = new Microsoft.AspNetCore.Builder.DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.htm");


            app.UseDefaultFiles(options);
            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions()
            {
                 ServeUnknownFileTypes = false // This allows files without extensions to be served
                ,DefaultContentType = "application/octet-stream" // Set a default content type for files without extensions
                ,OnPrepareResponse = AddStaticResponseHeaders
            });


            app.UseRouting();

            // app.UseAuthorization();


            // GET endpoint for the OCSP request with encoded parameter
            app.MapGet("/api/ocsp/{encoded}", OcspHandler.HandleGet);

            // POST endpoint for OCSP requests without a parameter
            app.MapPost("/api/ocsp", OcspHandler.HandlePost);
    


            // openssl ocsp -issuer ca_cert.pem -cert server_cert.pem -text -url http://ocsp.provider.com 

            // https://backreference.org/2010/05/09/ocsp-verification-with-openssl/
            // https://www.ietf.org/rfc/rfc2560.txt
            // curl http://some.ocsp.url/  > resp.der
            // openssl ocsp -respin resp.der - text

            // app.MapRazorPages();
            await app.RunAsync();

            return 0;
        } // End Task Main 


    } // End Class Program 


} // End Namespace 
