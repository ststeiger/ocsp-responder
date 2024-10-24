
namespace TestOCSP
{


    public class CertificateLoader
    {


        // .pfx or PKCS#12 files typically start with the following magic number in hexadecimal: 30 82
        // This represents the beginning of a DER-encoded sequence (ASN.1 structure) that is commonly used in PKCS#12 files.
        // However, this magic number is not exclusive to .pfx files;
        // other DER-encoded structures (like some X.509 certificates) can start similarly.
        public static bool IsPfxMagic(byte[] magicBytes)
        {
            if (magicBytes.Length < 2) // Too small to be a valid PFX file.
                return false;

            // PKCS#12 (.pfx) files often start with '30 82'
            return magicBytes[0] == 0x30 && magicBytes[1] == 0x82;
        } // End Function IsPfxMagic 


        public static bool IsPfxMagic(string filePath)
        {
            // Read the first two bytes to check the magic number
            byte[] magicBytes = new byte[2];

            using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                if (fs.Length < 2)
                    return false; // Too small to be a valid PFX file.

                fs.Read(magicBytes, 0, 2);
            }

            // PKCS#12 (.pfx) files often start with '30 82'
            return magicBytes[0] == 0x30 && magicBytes[1] == 0x82;
        } // End Function IsPfxMagic 


        // You're right, with the approach of catching a generic exception,
        // there's no distinction between whether the file is not a valid PFX file
        // or if the password provided is incorrect. 
        public static bool IsPfxFile(string filePath, string password, ref bool wrongPassword)
        {
            bool ret = false;

            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    Org.BouncyCastle.Pkcs.Pkcs12Store pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
                    pkcs12Store.Load(fs, password.ToCharArray());

                    ret = true; // Successfully parsed as a PFX file.
                }
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException)
            {
                // This exception usually indicates a wrong password.
                wrongPassword = true;
                ret = true; // File is a valid PFX, but the password is incorrect.
            }
            catch
            {
                // If an exception is thrown, it is not a valid PFX file.
                ret = false;
            }

            return ret;
        } // End Function IsPfxFile 


        public static (Org.BouncyCastle.X509.X509Certificate Certificate, Org.BouncyCastle.Crypto.AsymmetricKeyParameter PrivateKey)
            LoadPfx(string filePath, string password)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                Org.BouncyCastle.Pkcs.Pkcs12Store pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
                pkcs12Store.Load(fs, password.ToCharArray());


                string? alias = null;

                // Find the first key entry alias
                foreach (string name in pkcs12Store.Aliases)
                {
                    if (pkcs12Store.IsKeyEntry(name))
                    {
                        alias = name;
                        break;
                    } // End if (pkcs12Store.IsKeyEntry(name)) 

                } // Next name 

                if (alias == null)
                    throw new System.IO.FileNotFoundException("No private key entry found in the PFX file.");

                // Get the private key
                Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey = pkcs12Store.GetKey(alias).Key;

                // Get the certificate
                Org.BouncyCastle.Pkcs.X509CertificateEntry certEntry = pkcs12Store.GetCertificate(alias);
                Org.BouncyCastle.X509.X509Certificate certificate = certEntry.Certificate;

                return (certificate, privateKey);
            } // End Using fs 

        } // End Function LoadPfx 


        public static Org.BouncyCastle.X509.X509Certificate LoadCertificate(string filePath)
        {
            byte[] certBytes = System.IO.File.ReadAllBytes(filePath);

            // Try to parse as PEM first
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(certBytes))
            {

                using (System.IO.StreamReader reader = new System.IO.StreamReader(ms))
                {
                    Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(reader);
                    object pemObject = pemReader.ReadObject();

                    if (pemObject is Org.BouncyCastle.X509.X509Certificate pemCertificate)
                        return pemCertificate;
                } // End Using reader 

            } // End Using ms 

            // Fallback to DER parsing if PEM parsing fails
            Org.BouncyCastle.X509.X509CertificateParser parser = new Org.BouncyCastle.X509.X509CertificateParser();
            return parser.ReadCertificate(certBytes);
        }


        public static Org.BouncyCastle.X509.X509Certificate LoadDerCertificate(string filePath)
        {
            byte[] certBytes = System.IO.File.ReadAllBytes(filePath);
            Org.BouncyCastle.X509.X509CertificateParser parser = new Org.BouncyCastle.X509.X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate certificate = parser.ReadCertificate(certBytes);
            return certificate;
        } // End Function LoadDerCertificate 


    } // End Class CertificateLoader 


} // End Namespace 
