
namespace OCSPGuardian
{ 


    public class RepoClass 
        : OcspResponder.Core.IOcspResponderRepository
    {

        private System.Security.Cryptography.X509Certificates.X509Certificate2 m_rootCertificate;
        private System.Security.Cryptography.AsymmetricAlgorithm m_rootCertPrivateKey;


        public RepoClass()
        {
            // TODO: load root cert and private key from file 
            string location = "/etc/COR/skynet/skynet.pfx";

            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                location = @"D:\lolbot\CORaaaa\skynet\skynet.pfx";

            CertificateInfoDotNet cert = DotNetCertificateLoader.LoadPfxCertificate(location, "")!;
            this.m_rootCertificate = cert.Certificate!;
            this.m_rootCertPrivateKey = cert.PrivateKey!;
        } // End Constructor 


        void System.IDisposable.Dispose()
        {
            this.m_rootCertificate.Dispose();
            this.m_rootCertPrivateKey.Dispose();
        } // End Interface IDisposable.Dispose 



        // provides the chain of certificates up to the trusted root for the provided issuer certificate.
        async System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2[]>
            OcspResponder.Core.IOcspResponderRepository.GetChain(System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate)
        {
            // what's the result ? 
            return await System.Threading.Tasks.Task.FromResult(
                new System.Security.Cryptography.X509Certificates.X509Certificate2[] { issuerCertificate }
            );
        } // End Task GetChain 


        // The method should return a list of issuer certificates that the OCSP responder can validate.
        async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2>>
            OcspResponder.Core.IOcspResponderRepository.GetIssuerCertificates()
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2[] ls = new System.Security.Cryptography.X509Certificates.X509Certificate2[] {
                this.m_rootCertificate
            };

            return await System.Threading.Tasks.Task.FromResult(ls);
        } // End Task GetIssuerCertificates 


        // so basically, GetNextUpdate is a value that tells the client how long to cache the result until it needs to re-inquire
        async System.Threading.Tasks.Task<System.DateTimeOffset> OcspResponder.Core.IOcspResponderRepository.GetNextUpdate()
        {
            System.TimeSpan nextUpdateInterval = System.TimeSpan.FromDays(1); // TotalHours >= 1 && TotalDays < 28
            return await System.Threading.Tasks.Task.FromResult(System.DateTimeOffset.UtcNow + nextUpdateInterval);
        } // End Task GetNextUpdate 


        async System.Threading.Tasks.Task<System.Security.Cryptography.AsymmetricAlgorithm>
            OcspResponder.Core.IOcspResponderRepository.GetResponderPrivateKey(System.Security.Cryptography.X509Certificates.X509Certificate2 caCertificate)
        {
            return await System.Threading.Tasks.Task.FromResult(this.m_rootCertPrivateKey);
        } // End Task GetResponderPrivateKey 


        async System.Threading.Tasks.Task<OcspResponder.Core.CaCompromisedStatus> OcspResponder.Core.IOcspResponderRepository.IsCaCompromised(
            System.Security.Cryptography.X509Certificates.X509Certificate2 caCertificate
        )
        {
            OcspResponder.Core.CaCompromisedStatus stat = new OcspResponder.Core.CaCompromisedStatus();
            stat.IsCompromised = false;

            if(stat.IsCompromised)
                stat.CompromisedDate= System.DateTime.UtcNow;

            return await System.Threading.Tasks.Task.FromResult(stat);
        } // End Task IsCaCompromised 


        async System.Threading.Tasks.Task<bool> OcspResponder.Core.IOcspResponderRepository.SerialExists(
            string serial,
            System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate
        )
        {
            return await System.Threading.Tasks.Task.FromResult( true);
        } // End Task SerialExists


        async System.Threading.Tasks.Task<OcspResponder.Core.CertificateRevocationStatus>
            OcspResponder.Core.IOcspResponderRepository.SerialIsRevoked(
            string serial,
            System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate
        )
        {
            OcspResponder.Core.CertificateRevocationStatus stat = new OcspResponder.Core.CertificateRevocationStatus();
            stat.IsRevoked = false;

            if (stat.IsRevoked)
                stat.RevokedInfo = new OcspResponder.Core.RevokedInfo() { Date = System.DateTime.UtcNow, Reason = OcspResponder.Core.RevocationReason.AACompromise };

            //if (revocationTimestamp.Year < 1970) throw new ArgumentOutOfRangeException(nameof(revokedOn), revokedOn, "Invalid revocation timestamp.");

            return await System.Threading.Tasks.Task.FromResult(stat);
        } // End Task SerialIsRevoked


    } // End Class RepoClass 


} // End Namespace 
