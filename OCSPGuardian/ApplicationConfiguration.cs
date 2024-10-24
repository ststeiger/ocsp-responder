
namespace OCSPGuardian
{

    using Microsoft.Extensions.Configuration;

    public class ApplicationConfiguration
    {


        public static void Add(Microsoft.Extensions.Configuration.IConfigurationManager configuration, bool isWindows)
        {
            string launchSettings = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Properties", "launchSettings.json");
            configuration.AddJsonFile(launchSettings, optional: true);

            if (!isWindows) 
                configuration.AddJsonFile("hosting.json", optional: true, reloadOnChange: true);
        } // End Sub Add 


    } // End Class ApplicationConfiguration 


} // End Namespace 
