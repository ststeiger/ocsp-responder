
namespace OcspResponder.Core.Internal
{

    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Security;
    using X509Certificate = Org.BouncyCastle.X509.X509Certificate;


    /// <inheritdoc />
    internal class BcOcspResponderRepositoryAdapter : IBcOcspResponderRepository
    {
        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SerialExists(BigInteger serial, X509Certificate issuerCertificate)
        {
            var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(issuerCertificate.GetEncoded());
            return OcspResponderRepository.SerialExists(serial.ToString(), dotNetCertificate);
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<CertificateRevocationStatus> SerialIsRevoked(BigInteger serial, X509Certificate issuerCertificate)
        {
            var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(issuerCertificate.GetEncoded());
            return OcspResponderRepository.SerialIsRevoked(serial.ToString(), dotNetCertificate);
        }

        /// <param name="caCertificate"></param>
        /// <inheritdoc />
        public System.Threading.Tasks.Task<CaCompromisedStatus> IsCaCompromised(X509Certificate caCertificate)
        {
            var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertificate.GetEncoded());
            return OcspResponderRepository.IsCaCompromised(dotNetCertificate);
        }

        /// <param name="caCertificate"></param>
        /// <inheritdoc />
        public async System.Threading.Tasks.Task<AsymmetricKeyParameter> GetResponderPrivateKey(X509Certificate caCertificate)
        {
            var dotNetCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertificate.GetEncoded());
            var privateKey = await OcspResponderRepository.GetResponderPrivateKey(dotNetCertificate);
            return DotNetUtilities.GetKeyPair(privateKey).Private;
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task<AsymmetricKeyParameter> GetResponderPublicKey(X509Certificate caCertificate)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 dotNetCertificate = 
                new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertificate.GetEncoded());

            System.Security.Cryptography.AsymmetricAlgorithm privateKey = await OcspResponderRepository.GetResponderPrivateKey(dotNetCertificate);
            return DotNetUtilities.GetKeyPair(privateKey).Public;
        }

        /// <param name="issuerCertificate"></param>
        /// <inheritdoc />
        public async System.Threading.Tasks.Task<X509Certificate[]> GetChain(X509Certificate issuerCertificate)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 dotNetCertificate = 
                new System.Security.Cryptography.X509Certificates.X509Certificate2(issuerCertificate.GetEncoded());

            System.Security.Cryptography.X509Certificates.X509Certificate2[] certificates = 
                await OcspResponderRepository.GetChain(dotNetCertificate);


            // Allocate an array to hold the converted certificates
            X509Certificate[] convertedCertificates = new X509Certificate[certificates.Length];

            // Loop through each certificate and convert it
            for (int i = 0; i < certificates.Length; i++)
            {
                convertedCertificates[i] = DotNetUtilities.FromX509Certificate(certificates[i]);
            }

            return convertedCertificates;
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<X509Certificate>> GetIssuerCertificates()
        {
            System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2> certificates = 
                await OcspResponderRepository.GetIssuerCertificates();

            System.Collections.Generic.List<X509Certificate> convertedCertificates = new System.Collections.Generic.List<X509Certificate>();

            // Loop through each certificate and convert it
            foreach (System.Security.Cryptography.X509Certificates.X509Certificate2 certificate in certificates)
            {
                convertedCertificates.Add(DotNetUtilities.FromX509Certificate(certificate));
            }

            return convertedCertificates;
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task<System.DateTimeOffset> GetNextUpdate()
        {
            return await OcspResponderRepository.GetNextUpdate();
        }

        /// <see cref="OcspResponderRepository"/>
        private IOcspResponderRepository OcspResponderRepository { get; }

        internal BcOcspResponderRepositoryAdapter(IOcspResponderRepository ocspResponderRepository)
        {
            OcspResponderRepository = ocspResponderRepository;
        }
    }
}
