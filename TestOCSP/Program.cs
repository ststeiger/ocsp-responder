
namespace TestOCSP
{


    internal class Program
    {


        public static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            try
            {
                string endEntityCertPath = @"D:\my_certificate.cer";
                string issuerCertPath = @"D:\lolbot\CORaaaa\skynet\skynet.crt";


                // string location = @"D:\lolbot\CORaaaa\skynet\skynet.pfx";
                // CertificateInfoDotNet cert = DotNetCertificateLoader.LoadPfxCertificate(location, null)!;
                // System.Console.WriteLine(cert.Certificate);
                // System.Console.WriteLine(cert.PrivateKey!);

                Org.BouncyCastle.X509.X509Certificate endEntityCert = CertificateLoader.LoadDerCertificate(endEntityCertPath);
                Org.BouncyCastle.X509.X509Certificate issuerCert = CertificateLoader.LoadCertificate(issuerCertPath);


                OcspClient oc = new OcspClient();

                string? url = oc.GetAuthorityInformationAccessOcspUrl(endEntityCert);
                url = "http://ocsp.example.com";
                url = "https://localhost:7007/api/ocsp/";

                byte[] ocspResponse = await oc.QueryBinary(endEntityCert, issuerCert, url);
                if (ocspResponse == null)
                    throw new System.ArgumentNullException("ocspResponse cannot be NULL");

                CertificateStatus certStatus = oc.ProcessOcspResponse(ocspResponse);
                System.Console.WriteLine(certStatus.ToString());
            } // End Try 
            catch (System.Exception ex)
            {
                System.Console.WriteLine(System.Environment.NewLine);
                System.Console.WriteLine(System.Environment.NewLine);
                System.Console.WriteLine("=====================================");
                System.Console.WriteLine("ERROR");
                System.Console.WriteLine("=====================================");
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            } // End Catch 

            System.Console.WriteLine(" --- Press any key to continue --- ");
            System.Console.ReadKey();

            return 0;
        } // End Task Main 


    } // End Class Program 


} // End Namespace 
