
namespace OCSPGuardian
{

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;


    public static class ApplicationHttpsRedirectPolicy
    {


        public static void ConfigureHttpsRedirection(
            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration
        )
        {
            services.AddHttpsRedirection(
                delegate (Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions options)
                {
                    options.RedirectStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status308PermanentRedirect;
                    // Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect

                    bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                    // https://stackoverflow.com/questions/42272021/check-if-asp-netcore-application-is-hosted-in-iis
                    // https://docs.microsoft.com/en-us/previous-versions/iis/6.0-sdk/ms524602%28v%3Dvs.90%29
                    bool usesIIS = (isWindows && System.Environment.GetEnvironmentVariable("APP_POOL_ID") is string) ? true : false;
                    bool behindReverseProxy = configuration.GetValue<bool>("Kestrel:BehindReverseProxy", false);
                    bool useKestrel = usesIIS ? false : true;


                    if (useKestrel)
                    {
                        libWebAppBasics.PseudoUrl url = configuration.GetValue<string?>("Kestrel:EndPoints:Https:Url", null)!;
                        if (url != null)
                            options.HttpsPort = url.Port;
                        else
                            options.HttpsPort = 5005;
                    }
                    else
                    {
                        // PseudoUrl url = this.Configuration.GetValue<string>("iisSettings:iisExpress:applicationUrl", null);
                        options.HttpsPort = configuration.GetValue<int>("iisSettings:iisExpress:sslPort", 443);
                    }

                    if (options.HttpsPort == 0)
                        options.HttpsPort = 443;

                    if (useKestrel && behindReverseProxy)
                        options.HttpsPort = 443;
                }
            );

        } // End Function ConfigureHttpsRedirection 


    } // End Class ApplicationHttpsRedirectPolicy 


} // End Namespace 
