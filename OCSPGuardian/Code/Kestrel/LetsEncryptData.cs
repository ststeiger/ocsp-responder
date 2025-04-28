
namespace OCSPGuardian
{


    public class LetsEncryptData
    {

        public const string DEFAULT_PASSWORD = nameof(LetsEncryptData);


        public bool UseLetsEncrypt;
        public string Domain;
        public string Token;
        public string SignedNonce;
        public string PfxPassword;


        protected bool m_isNotWindows;
        protected System.Security.Cryptography.X509Certificates.X509Certificate2 m_certificate;
        protected byte[] m_bkcs12Bytes;


        public LetsEncryptData(string password, string domain, bool useLetsEncrypt)
        {
            this.Domain = domain.ToLowerInvariant();
            this.UseLetsEncrypt = useLetsEncrypt;
            this.m_isNotWindows = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            this.PfxPassword = password;
        } // End Constructor 


        public LetsEncryptData(string domain, bool useLetsEncrypt)
            : this(DEFAULT_PASSWORD, domain, useLetsEncrypt)
        { }


        public LetsEncryptData(string domain)
            : this(DEFAULT_PASSWORD, domain, true)
        { }


        public LetsEncryptData()
            : this(DEFAULT_PASSWORD, "localhost", true)
        { }


        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate
        {
            get
            {
                if (this.m_isNotWindows)
                    return this.m_certificate;

                // Hack for 2017 Windoze Bug "No credentials are available in the security package" 
                // SslStream is not working with ephemeral keys ... 
                System.Security.Cryptography.X509Certificates.X509Certificate2 cert =
                    new System.Security.Cryptography.X509Certificates.X509Certificate2(this.m_bkcs12Bytes, this.PfxPassword);
                return cert;
            }

            set
            {
                this.m_certificate = value;

                if (this.m_certificate != null)
                    this.m_bkcs12Bytes = this.m_certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, this.PfxPassword);
            }

        } // End Property Certificate 


        public LetsEncryptData FromPem(string cert, string key)
        {
            System.ReadOnlySpan<char> cert_pem = System.MemoryExtensions.AsSpan(cert);
            System.ReadOnlySpan<char> private_key = System.MemoryExtensions.AsSpan(key);

            System.Security.Cryptography.X509Certificates.X509Certificate2 sslCert =
                System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(cert_pem, private_key);

            this.Certificate = sslCert;

            return this;
        } // End Function FromPem 


        public LetsEncryptData FromPfx(byte[] pfx, string password)
        {
            this.PfxPassword = password;

            System.Security.Cryptography.X509Certificates.X509Certificate2 sslCert =
                new System.Security.Cryptography.X509Certificates.X509Certificate2(pfx, password, System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable);

            this.Certificate = sslCert;

            return this;
        } // End Function FromPfx 


        public LetsEncryptData FromPfx(byte[] pfx)
        {
            return this.FromPfx(pfx, DEFAULT_PASSWORD);
        } // End Function FromPfx 


    } // End Class LetsEncryptData 


} // End Namespace HomepageDaniel 
