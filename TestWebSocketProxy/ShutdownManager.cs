
using System.DirectoryServices.ActiveDirectory;

namespace TestWebSocketProxy
{


    // Shutdown and restart now 
    // shutdown /r /t 0
    // Force shutdown and restart now 
    // shutdown /r /f /t 0
    // Shutdown and restart in a minute with message 
    // shutdown /r /t 60 /c "Server will restart in 1 minute"
    class ShutdownManager 
    {


        private static string GetDomain()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                return System.Environment.MachineName;

            System.Management.ManagementObjectSearcher searcher = 
                new System.Management.ManagementObjectSearcher("SELECT Domain, PartOfDomain FROM Win32_ComputerSystem");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                bool partOfDomain = (bool)obj["PartOfDomain"];
                string? domain = obj["Domain"].ToString();
                System.Console.WriteLine("Part of domain: " + partOfDomain);
                System.Console.WriteLine("Domain: " + domain);

                if (partOfDomain)
                    return domain ?? System.Environment.MachineName;
            } // Next obj 

            return System.Environment.MachineName;
        } // End Function GetDomain 


        private static string GetAdDomain()
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                return System.Environment.MachineName;
            System.DirectoryServices.ActiveDirectory.Domain domain = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();
            string? domainName = domain?.Name ?? System.Environment.MachineName;

            return domainName;
        } // End Function GetAdDomain 


        public static void ShutdownNow()
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo() 
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 0",
                    UseShellExecute = true, 
                    CreateNoWindow= true
                }
            );
        } // End Sub ShutdownNow 


        public static void ShutdownWithPrompt()
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "shutdown",
                    Arguments = "/r /t 0",
                    UseShellExecute = true,
                    Verb = "runas" // prompts for elevation
                }
            );
        } // End Sub ShutdownWithPrompt 


        public static void ShutdownWithoutPrompt()
        {
            // string mgmtDomain = GetDomain();
            string domainName = GetAdDomain();


            System.Diagnostics.ProcessStartInfo psi = 
                new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "shutdown.exe",
                    Arguments = "/r /t 0",
                    UserName = "Administrator",
                    UseShellExecute = false,
                }
            ;


            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // psi.Domain = "YOUR_DOMAIN", // or computer name if local account
                psi.Domain = domainName;

                psi.LoadUserProfile = true;

                System.Security.SecureString password = new System.Security.SecureString();
                foreach (char c in "YourPassword") password.AppendChar(c);
                psi.Password = password;
            } // End if Windows 
            
            System.Diagnostics.Process.Start(psi);
        } // End Sub ShutdownWithoutPrompt 


    } // End Class ShutdownManager 


} // End Namespace 
