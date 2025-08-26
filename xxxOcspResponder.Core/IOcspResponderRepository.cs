
namespace OcspResponder.Core
{
    /// <summary>
    /// Contract that an OCSP Responder uses to validate a certificate in a CA repository
    /// </summary>
    public interface IOcspResponderRepository 
        : System.IDisposable
    {
        /// <summary>
        /// Checks whether the serial exists for this CA repository
        /// </summary>
        /// <param name="serial">serial</param>
        /// <param name="issuerCertificate"></param>
        /// <returns><c>true</c> if the serial exists; otherwise, false</returns>
        System.Threading.Tasks.Task<bool> SerialExists(string serial, System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate);

        /// <summary>
        /// Checks whether the serial is revoked for this CA repository.
        /// </summary>
        /// <param name="serial">serial</param>
        /// <param name="issuerCertificate"></param>
        /// <returns>A <see cref="CertificateRevocationStatus"/> containing whether the certificate is revoked and more info</returns>
        System.Threading.Tasks.Task<CertificateRevocationStatus> SerialIsRevoked(
            string serial, 
            System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate);

        /// <summary>
        /// Checks whether the CA is compromised.
        /// </summary>
        /// <param name="caCertificate"></param>
        /// <returns>A <see cref="CaCompromisedStatus"/> containing whether the CA is revoked and when it happens</returns>
        System.Threading.Tasks.Task<CaCompromisedStatus> IsCaCompromised(System.Security.Cryptography.X509Certificates.X509Certificate2 caCertificate);

        /// <summary>
        /// Gets the private key of the CA or its designated responder
        /// </summary>
        /// <param name="caCertificate"></param>
        /// <returns>A <see cref="AsymmetricAlgorithm"/> that represents the private key of the CA</returns>
        System.Threading.Tasks.Task<System.Security.Cryptography.AsymmetricAlgorithm> GetResponderPrivateKey(
            System.Security.Cryptography.X509Certificates.X509Certificate2 caCertificate);

        /// <summary>
        /// The certificate chain associated with the response signer.
        /// </summary>
        /// <param name="issuerCertificate"></param>
        /// <returns>An array of <see cref="X509Certificate2"/></returns>
        System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2[]> GetChain(
            System.Security.Cryptography.X509Certificates.X509Certificate2 issuerCertificate
        );

        /// <summary>
        /// Gets the date when the client should request the responder about the certificate status
        /// </summary>
        /// <returns>A <see cref="DateTime"/> that represents when the client should request the responder again</returns>
        System.Threading.Tasks.Task<System.DateTimeOffset> GetNextUpdate();

        /// <summary>
        /// Gets the issuer certificate that this repository is responsible to evaluate
        /// </summary>
        /// <returns>A <see cref="X509Certificate2"/> that represents the issuer's certificate</returns>
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<System.Security.Cryptography.X509Certificates.X509Certificate2>> GetIssuerCertificates();
    }
}
