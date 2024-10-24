
namespace TestOCSP
{


    internal static class CertificateExtensions
    {


        public static System.Security.Cryptography.AsymmetricAlgorithm? GetPrivateKey(this System.Security.Cryptography.X509Certificates.X509Certificate2 cert2)
        {
            if (!cert2.HasPrivateKey)
                return null;

            // string ka = cert2.GetKeyAlgorithm(); ;
            // System.Security.Cryptography.AsymmetricAlgorithm? pk = cert2.PrivateKey;

            

            System.Security.Cryptography.AsymmetricAlgorithm? a = System.Security.Cryptography.X509Certificates.RSACertificateExtensions.GetRSAPrivateKey(cert2);

            if (a != null)
                return a;
            
            System.Security.Cryptography.AsymmetricAlgorithm? b = System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions.GetECDsaPrivateKey(cert2);

            if (b != null)
                return b;

            
            System.Security.Cryptography.AsymmetricAlgorithm? c = System.Security.Cryptography.X509Certificates.DSACertificateExtensions.GetDSAPrivateKey(cert2);
            if (c != null)
                return c;

            System.Security.Cryptography.AsymmetricAlgorithm? d = cert2.GetECDiffieHellmanPrivateKey();

            return d;
        } // End Function GetPrivateKey 


    }


}
