
namespace ReportServerProxyFF
{


    public class SslHack
    {


        public static bool IgnoreCertificateCheck(
            object sender, 
            System.Security.Cryptography.X509Certificates.X509Certificate certificate, 
            System.Security.Cryptography.X509Certificates.X509Chain chain, 
            System.Net.Security.SslPolicyErrors sslPolicyErrors
        )
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                return true;

            bool bIgnoreSslErrors = true; // Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreSslErrors"]);

            if (bIgnoreSslErrors)
            {
                // allow any old dodgy certificate…
                return true;
            }
            else
            {
                // certificate.GetExpirationDateString();
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }

        } // End Function IgnoreCertificateCheck



        // http://blog.jameshiggs.com/2008/05/01/c-how-to-accept-an-invalid-ssl-certificate-programmatically/
        // http://social.msdn.microsoft.com/Forums/en-US/sqlreportingservices/thread/4819523b-31c5-44db-8b9f-1f4e0c9aa0c9
        // http://stackoverflow.com/questions/1301127/how-to-ignore-a-certificate-error-with-c-2-0-webclient-without-the-certificate
        public static void InitiateSSLTrust()
        {

            try
            {
                // try TLS 1.3
                // But Tls11, Tls12, and Tls13 enums are not part of .NET 4.0.
                // However, because.NET is so closely integrated with Windows,
                // sometime it’s worth asking it directly – by specifying enum’s numerical value. 
                // If you run this code before making the first HTTP request,
                // suddenly you are not limited to SSL and the ancient TLS anymore.
                System.Net.ServicePointManager.SecurityProtocol =
                                       (System.Net.SecurityProtocolType)12288 // System.Net.SecurityProtocolType.Tls13
                                     | (System.Net.SecurityProtocolType)3072 // System.Net.SecurityProtocolType.Tls12
                                     | (System.Net.SecurityProtocolType)768 // System.Net.SecurityProtocolType.Tls11
                                     | System.Net.SecurityProtocolType.Tls;

                System.Console.WriteLine("Using TLS 1.3");
            }
            catch (System.NotSupportedException)
            {
                // This code still requires a bit of error checking
                try
                {
                    // Try TLS 1.2
                    System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072
                                                         | (System.Net.SecurityProtocolType)768
                                                         | System.Net.SecurityProtocolType.Tls;
                    System.Console.WriteLine("Using TLS 1.2");
                }
                catch (System.NotSupportedException)
                {
                    try
                    {
                        // Try TLS 1.1
                        System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)768
                                                             | System.Net.SecurityProtocolType.Tls;
                        System.Console.WriteLine("Using TLS 1.1");
                    }
                    catch (System.NotSupportedException)
                    {
                        // TLS 1.0
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                        System.Console.WriteLine("Using TLS 1.0");
                    }
                }
            }


            try
            {
                // System.Net.ServicePointManager.ServerCertificateValidationCallback = Function() True
                // System.Net.ServicePointManager.ServerCertificateValidationCallback = () => true;
                // System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(() => true);

                // .NET 4.0+
                // System.Net.HttpWebRequest request = System.Net.HttpWebRequest.CreateHttp("https://www.stackoverflow.com"); 
                // request.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
                // End .NET 4.0+


                System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(IgnoreCertificateCheck);
                // System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
                // System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });
                //System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

        } // End Function InitiateSSLTrust


    }
}