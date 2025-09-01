
namespace EnvoyPlaneController
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

            Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
