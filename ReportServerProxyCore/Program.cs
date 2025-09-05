
namespace ReportServerProxyCore
{

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;


    public class Program
    {


        public static void Main(string[] args)
        {
            Microsoft.AspNetCore.Builder.WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // builder.Services.AddHttpClient();

            builder.Services.AddHttpClient("ReportProxy", client =>
            {
                // client.DefaultRequestHeaders.ExpectContinue = false; // Turn off if needed
            })
            .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler()
            {
                // Expect100Continue = false, // Explicit
                AllowAutoRedirect = false,

                AutomaticDecompression =
                           System.Net.DecompressionMethods.GZip |
                           System.Net.DecompressionMethods.Deflate |
                           System.Net.DecompressionMethods.Brotli
            });

            //builder.Services.AddHttpClient().ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler()
            //{
            //    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli
            //});




            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();




            Microsoft.AspNetCore.Builder.DefaultFilesOptions options = new Microsoft.AspNetCore.Builder.DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.htm");


            app.UseDefaultFiles(options);

            app.UseStaticFiles();

            /*
            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions()
            {
                ServeUnknownFileTypes = false // This allows files without extensions to be served
                ,
                DefaultContentType = "application/octet-stream" // Set a default content type for files without extensions
                ,
                OnPrepareResponse = AddStaticResponseHeaders
            });
            */


            app.UseRouting();

            app.UseAuthorization();

            /*

            app.UseEndpoints(endpoints =>
            {
                // Other endpoints...

                // Catch-all for /ReportServer/*
                endpoints.Map("/{**path}", async context =>
                {
                    var path = context.Request.Path.Value;
                    if (path.StartsWith("/ReportServer", StringComparison.OrdinalIgnoreCase))
                    {
                        var middleware = context.RequestServices.GetRequiredService<ReportProxyMiddleware>();
                        await middleware.Invoke(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not Found");
                    }
                });
            });
            */

            // app.UseMiddleware<ReportProxyMiddleware>();


            // app.MapReportProxy("/{*catchAll}");
            app.MapReportProxy("/blablaReportServer/{*catchAll}");



            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        } // End Sub Main 


    } // End Class Program 


} // End Namespace 

