
namespace OCSPGuardian
{


    public class CertificateInfoDotNet
    {
        public System.Security.Cryptography.X509Certificates.X509Certificate2? Certificate;
        public System.Security.Cryptography.AsymmetricAlgorithm? PrivateKey;


        public CertificateInfoDotNet(
             System.Security.Cryptography.X509Certificates.X509Certificate2? certificate
            , System.Security.Cryptography.AsymmetricAlgorithm? privateKey
        )
        {
            this.Certificate = certificate;
            this.PrivateKey = privateKey;
        } // End Constructo 

        public CertificateInfoDotNet()
            : this(null, null)
        { } // End Constructo 

    } // End Class CertificateInfoDotNet 


    internal class DotNetCertificateLoader 
    {

        public static CertificateInfoDotNet? LoadPfxCertificate(string pfxLocation, string password)
        {

            System.Security.Cryptography.X509Certificates.X509Certificate2 cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                pfxLocation,
                password,
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet |
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable
            );
            // cert2.PrivateKey = null;

            if (cert2.HasPrivateKey)
            {
                return new CertificateInfoDotNet(cert2, CertificateExtensions.GetPrivateKey(cert2));
            } // End if (cert2.HasPrivateKey) 

            return null;
        } // End Function LoadCertificateUsingDotNet 


        public static CertificateInfoDotNet LoadPfxCertificate(byte[] pfxBytes, string password)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                pfxBytes,
                password,
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet |
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable
            );

            // Get the private key (if available)
            System.Security.Cryptography.AsymmetricAlgorithm? privateKey = System.Security.Cryptography.X509Certificates
                .RSACertificateExtensions.GetRSAPrivateKey(certificate);

            if (privateKey != null)
            {
                System.Console.WriteLine("Private Key: " + privateKey.ToXmlString(true)); // Export as XML (for RSA keys)
            } // End if (privateKey != null) 

            // Display certificate details
            System.Console.WriteLine("Subject: " + certificate.Subject);
            System.Console.WriteLine("Issuer: " + certificate.Issuer);
            System.Console.WriteLine("Expiration: " + certificate.NotAfter);

            return new CertificateInfoDotNet(certificate, privateKey);
        } // End Sub LoadCertificateUsingDotNet 


        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadDerCertificate(string filePath)
        {
            byte[] certBytes = System.IO.File.ReadAllBytes(filePath);
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(certBytes);
        } // End Function LoadDerCertificate 


        private static string ExtractBase64FromPem(string pemContent)
        {
            // Remove PEM headers
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (string line in pemContent.Split('\n'))
            {
                if (!line.StartsWith("-----"))
                    sb.Append(line.Trim());
            } // Next line 

            return sb.ToString();
        } // End Function ExtractBase64FromPem 


        public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadPemCertificate(string filePath)
        {
            string pemContent = System.IO.File.ReadAllText(filePath);
            string base64Cert = ExtractBase64FromPem(pemContent);
            byte[] certBytes = System.Convert.FromBase64String(base64Cert);
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(certBytes);
        } // End Function LoadPemCertificate 


    } // End Class DotNetCertificateLoader 


} // End Namespace 
