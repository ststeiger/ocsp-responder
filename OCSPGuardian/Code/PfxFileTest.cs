
namespace OCSPGuardian.Code
{


    public class PfxFileTest 
    {


        public static byte[]? CreatePfxFile(
            Org.BouncyCastle.X509.X509Certificate cert,
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey,
            string alias,
            string password
        )
        {
            Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder builder = 
                new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder();

            Org.BouncyCastle.Pkcs.Pkcs12Store store = builder
                .SetUseDerEncoding(true) // Better security
                .SetKeyAlgorithm(
                    Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers
                    .PbeWithShaAnd3KeyTripleDesCbc
                ) // or AES256-CBC
                .SetCertAlgorithm(
                    Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers
                    .PbewithShaAnd40BitRC2Cbc
                ) // or AES128-CBC
            .Build();

            Org.BouncyCastle.Security.SecureRandom random = 
                new Org.BouncyCastle.Security.SecureRandom(
                    new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator()
            );

            // string alias = "mykey";

            store.SetKeyEntry(alias,
                new Org.BouncyCastle.Pkcs.AsymmetricKeyEntry(privateKey),
                new Org.BouncyCastle.Pkcs.X509CertificateEntry[] { 
                    new Org.BouncyCastle.Pkcs.X509CertificateEntry(cert) 
                }
            );

            byte[]? pfxBytes = null;

            // Save to memory
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                store.Save(
                    stream,
                    password.ToCharArray(), 
                    random
                );
                pfxBytes = stream.ToArray();
            } // End Using stream 

            return pfxBytes;
        } // End Function CreatePfxFile 


    } // End Class PfxFileTest 


} // End Namespace 

