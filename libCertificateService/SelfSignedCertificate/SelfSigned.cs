
using libWebAppBasics;
using SelfSignedCertificateGenerator;


namespace SimpleChallengeResponder
{


    public class SelfSigned
    {


        public static System.Collections.Generic.List<string> GetAlternativeNames(
            params string[] otherDomains
        )
        {
            // http://localhost
            // http://machine-name
            // http://[::1]
            // http://127.0.0.1
            // http://publicIp

            // Yep. Cloudflare uses it for its DNS instructions homepage: https://1.1.1.1
            // https://1.1.1.1/

            string strHostName = System.Net.Dns.GetHostName();
            System.Net.IPHostEntry iphostentry = System.Net.Dns.GetHostEntry(strHostName);

            System.Collections.Generic.List<string> ls = new System.Collections.Generic.List<string>();
            ls.Add("localhost");
            ls.AddRange(otherDomains);
            ls.Add(System.Environment.MachineName);
            ls.Add("127.0.0.1"); // IPv4 Loopback 
            ls.Add("::1"); // IPv6 Loopback 
            // , "[::1]" // IPv6 Loopback // error
            // , "sql.guru", "*.sql.guru", "example.int", "foo.int", "bar.int", "foobar.int"
            // , "*.com" // not supported 

            // Enumerate IP addresses
            foreach (System.Net.IPAddress ipAddress in iphostentry.AddressList)
            {
                // https://superuser.com/questions/99746/why-is-there-a-percent-sign-in-the-ipv6-address
                // The number after the '%' is the scope ID.

                // The percent character(%) in the IPv6 literal address
                // must be percent escaped when present in the URI. 
                // For example, the scope ID FE80::2 % 3, 
                // must appear in the URI as "https://[FE80::2%253]/", 
                // where % 25 is the hex encoded percent character(%).

                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && ipAddress.ScopeId != 0)
                    continue;

                string ipString = ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4().ToString():ipAddress.ToString();

                if (ls.FindIndex(x => x.Equals(ipString, System.StringComparison.InvariantCultureIgnoreCase))!= -1)
                    ls.Add(ipString);
            } // Next ipAddress 

#if false

            System.Net.NetworkInformation.NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (System.Net.NetworkInformation.NetworkInterface nic in nics)
            {
                if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    System.Console.WriteLine(nic.GetIPProperties().UnicastAddresses);
                }


                if (nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                    && nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel
                    && nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                    && nic.Name.StartsWith("vEthernet") == false
                    && nic.Description.Contains("Hyper-v") == false)
                {
                    //Do something
                    break;
                }
            }
#endif 

            return ls;
        } // End Function GetAlternativeNames 


        public static System.Collections.Generic.List<string> GetAlternativeNames()
        {
            return GetAlternativeNames(new string[0]);
        } // End Function GetAlternativeNames 


        public static byte[] CreateSelfSignedCertificate(
            System.Collections.Generic.IEnumerable<string> alternativeNames, 
            string password
        )
        {
            string pemKey = SecretManager.GetSecretOrThrow<string>("skynet_key");
            string pemCert = SecretManager.GetSecretOrThrow<string>("skynet_cert");

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair rootKey = ReadAsymmetricKeyParameter(pemKey);
            Org.BouncyCastle.X509.X509Certificate rootCert = PemStringToX509(pemCert);

            Org.BouncyCastle.Security.SecureRandom random = new Org.BouncyCastle.Security.SecureRandom(NonBackdooredPrng.Create());
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair certKeyPair = KeyGenerator.GenerateRsaKeyPair(2048, random);

            Org.BouncyCastle.X509.X509Certificate sslCertificate = SelfSignSslCertificate(
                  random
                , rootCert
                , certKeyPair.Public
                , rootKey.Private
                , alternativeNames
            );

            bool val = CerGenerator.ValidateSelfSignedCert(sslCertificate, rootCert.GetPublicKey());
            if (val == false)
                throw new System.InvalidOperationException("SSL certificate does NOT validate successfully.");

            byte[] pfx = CreatePfxBytes(sslCertificate, certKeyPair.Private, password);
            return pfx;
        } // End Function CreateSelfSignedCertificate 


        public static byte[] CreateSelfSignedCertificate(string password)
        {
            System.Collections.Generic.List<string> altNames = GetAlternativeNames();
            return CreateSelfSignedCertificate(altNames, password);
        }


        private static Org.BouncyCastle.X509.X509Certificate SelfSignSslCertificate(
              Org.BouncyCastle.Security.SecureRandom random
            , Org.BouncyCastle.X509.X509Certificate caRoot
            , Org.BouncyCastle.Crypto.AsymmetricKeyParameter subjectPublicKey
            , Org.BouncyCastle.Crypto.AsymmetricKeyParameter rootCertPrivateKey
            , System.Collections.Generic.IEnumerable<string> alternativeNames 
        ) 
        {
            Org.BouncyCastle.X509.X509Certificate caSsl;

            string countryIso2Characters = "GA";
            string stateOrProvince = "Aremorica";
            string localityOrCity = "Erquy, Bretagne";
            string companyName = "Coopérative Ménhir Obelix GmbH & Co. KGaA";
            string division = "Neanderthal Technology Group (NT)";
            string domainName = "localhost";
            string email = "webmaster@localhost";


            CertificateInfo ci = new CertificateInfo(
                  countryIso2Characters, stateOrProvince
                , localityOrCity, companyName
                , division, domainName, email
                , System.DateTime.UtcNow
                , System.DateTime.UtcNow.AddYears(50)
            );

            
            ci.AddAlternativeNames(alternativeNames);

            // Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp1 = KeyGenerator.GenerateEcKeyPair(curveName, random);
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp1 = KeyGenerator.GenerateRsaKeyPair(2048, random);
            // Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp1 = KeyGenerator.GenerateDsaKeyPair(1024, random);
            // Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp1 = KeyGenerator.GenerateDHKeyPair(1024, random);


            caSsl = CerGenerator.GenerateSslCertificate(
                  ci
                , subjectPublicKey
                , rootCertPrivateKey
                , caRoot
                , random
            );

            return caSsl;
        } // End Sub SelfSignSslCertificate 


        public static byte[] CreatePfxBytes(
              Org.BouncyCastle.X509.X509Certificate certificate
            , Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateKey
            , string password = "")
        {
            byte[]? pfxBytes = null;

            // create certificate entry
            Org.BouncyCastle.Pkcs.X509CertificateEntry certEntry =
                new Org.BouncyCastle.Pkcs.X509CertificateEntry(certificate);

            Org.BouncyCastle.Asn1.X509.X509Name name = new Org.BouncyCastle.Asn1.X509.X509Name(certificate.SubjectDN.ToString());
            string friendlyName = (string)name.GetValueList(Org.BouncyCastle.Asn1.X509.X509Name.O)[0];

            if (System.StringComparer.InvariantCultureIgnoreCase.Equals("Skynet Earth Inc.", friendlyName))
                friendlyName = "Skynet Certification Authority";

            
            Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder builder = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder();
            builder.SetUseDerEncoding(true);


            Org.BouncyCastle.Pkcs.Pkcs12Store store = builder.Build();

            store.SetCertificateEntry(friendlyName, certEntry);

            // create store entry
            store.SetKeyEntry(
                  friendlyName
                , new Org.BouncyCastle.Pkcs.AsymmetricKeyEntry(privateKey)
                , new Org.BouncyCastle.Pkcs.X509CertificateEntry[] { certEntry }
            );

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                // Cert is contained in store
                // null: no password, "": an empty passwords
                // note: Linux needs empty password on null...
                store.Save(stream, password == null ? "".ToCharArray() : password.ToCharArray(), new Org.BouncyCastle.Security.SecureRandom());
                // stream.Position = 0;
                pfxBytes = Org.BouncyCastle.Pkcs.Pkcs12Utilities.ConvertToDefiniteLength(stream.ToArray());
            } // End Using stream 

            return pfxBytes;
        } // End Function CreatePfxBytes 


        private static Org.BouncyCastle.X509.X509Certificate PemStringToX509(string pemString)
        {
            Org.BouncyCastle.X509.X509Certificate cert;
            Org.BouncyCastle.X509.X509CertificateParser kpp = new Org.BouncyCastle.X509.X509CertificateParser();

            using (System.IO.Stream pemStream = new StringStream(pemString))
            {
                cert = kpp.ReadCertificate(pemStream);
            } // End Using pemStream 

            return cert;
        } // End Function PemStringToX509 


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


    } // End Class SelfSigned 


} // End Namespace HomepageDaniel 
