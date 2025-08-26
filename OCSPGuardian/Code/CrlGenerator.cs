
namespace OCSPGuardian
{

    using libWebAppBasics;


    public static class CrlGenerator
    {


        private static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair ReadAsymmetricKeyParameter(System.IO.TextReader textReader)
        {
            Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair KeyParameter = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pemReader.ReadObject();
            return KeyParameter;
        } // End Function ReadAsymmetricKeyParameter 


        private static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair ReadAsymmetricKeyParameter(string pemString)
        {
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair para;

            using (System.IO.TextReader tr = new System.IO.StringReader(pemString))
            {
                para = ReadAsymmetricKeyParameter(tr);
            } // End Using tr 

            return para;
        } // End Function ReadAsymmetricKeyParameter 


        private static Org.BouncyCastle.X509.X509Certificate PemStringToX509(string pemString)
        {
            byte[] foo = System.Text.Encoding.UTF8.GetBytes(pemString);
            Org.BouncyCastle.X509.X509CertificateParser kpp = 
                new Org.BouncyCastle.X509.X509CertificateParser();

            Org.BouncyCastle.X509.X509Certificate cert = kpp.ReadCertificate(foo);
            
            return cert;
        } // End Function PemStringToX509 


        private static byte[] GenerateCrl(
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter caPrivateKey,
            Org.BouncyCastle.X509.X509Certificate caCertificate,
            System.Collections.Generic.IEnumerable<
                Org.BouncyCastle.Math.BigInteger
            >? revokedSerialNumbers
        )
        {
            System.DateTime now = System.DateTime.UtcNow;
            System.DateTime nextUpdate = now.AddDays(7); // Next CRL update

            Org.BouncyCastle.X509.X509V2CrlGenerator crlGen = 
                new Org.BouncyCastle.X509.X509V2CrlGenerator();

            // Set issuer (must match CA exactly)
            crlGen.SetIssuerDN(caCertificate.SubjectDN);

            crlGen.SetThisUpdate(now);
            crlGen.SetNextUpdate(nextUpdate);


            long unixTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Org.BouncyCastle.Math.BigInteger revocationListSerialNumber = 
                new Org.BouncyCastle.Math.BigInteger(unixTimestamp.ToString());

            // Optional: Add CRL Number extension
            crlGen.AddExtension(
                Org.BouncyCastle.Asn1.X509.X509Extensions.CrlNumber,
                critical: false,
                new Org.BouncyCastle.Asn1.X509.CrlNumber(
                    revocationListSerialNumber
                )
            );

            // Add revoked certs if any
            if (revokedSerialNumbers != null)
            {
                foreach (Org.BouncyCastle.Math.BigInteger serial in revokedSerialNumbers)
                {
                    crlGen.AddCrlEntry(
                        userCertificate: serial,
                        revocationDate: now,
                        reason: Org.BouncyCastle.Asn1.X509.CrlReason.PrivilegeWithdrawn // Example reason
                    );
                } // Next serial 

            } // End if (revokedSerialNumbers != null) 

            // Signer
            Org.BouncyCastle.Crypto.ISignatureFactory signatureFactory = 
                new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory(
                    "SHA256WITHRSA", caPrivateKey
            );

            // Generate the CRL
            Org.BouncyCastle.X509.X509Crl crl = crlGen.Generate(signatureFactory);

            // Export as DER
            return crl.GetEncoded();
        } // End Function GenerateCrl 


        private static byte[] GenerateCrl(
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter caPrivateKey,
            Org.BouncyCastle.X509.X509Certificate caCertificate
        )
        {
            return GenerateCrl(caPrivateKey, caCertificate, null);
        } // End Function GenerateCrl 


        public static async System.Threading.Tasks.Task HandleGet(
           Microsoft.AspNetCore.Http.HttpContext context
        )
        {
            string pemKey = SecretManager.GetSecretOrThrow<string>("skynet_key");
            string pemCert = SecretManager.GetSecretOrThrow<string>("skynet_cert");

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair caCipherKeyPair = ReadAsymmetricKeyParameter(pemKey);
            Org.BouncyCastle.X509.X509Certificate caCertificate = PemStringToX509(pemCert);

            // System.Collections.Generic.IEnumerable<Org.BouncyCastle.Math.BigInteger>? revokedSerialNumbers revokedCerts = await GetRevokedSerialsFromDbAsync(); // your DB code
            // byte[] crl = GenerateCrl(caPrivateKey, caCertificate, revokedCerts);
            byte[] crl = GenerateCrl(caCipherKeyPair.Private, caCertificate, null);

            context.Response.ContentType = "application/pkix-crl";
            context.Response.Headers.CacheControl = "public, max-age=300";
            await context.Response.Body.WriteAsync(crl, 0, crl.Length);
        } // End Task HandleGet 


    } // End Class CrlGenerator 


} // End Namespace 
