
namespace libCertificateService
{


    public interface ICertificateRepository
    {
        /// <summary>
        /// Gets all currently valid certificates for all domains 
        /// </summary>
        System.Collections.Generic.List<Certificate> GetAllValidCertificates();
    } // End Interface ICertificateRepository 


    public interface ICertificateService
    {

        /// <summary>
        /// Gets TLS-Certificate for domain
        /// </summary>
        /// <param name="domainName">the sni-name/san-name/IP/unix-socket-path of the TLS-connection</param>
        /// <returns></returns>
        System.Security.Cryptography.X509Certificates.X509Certificate2? GetCertificate(string domainName);

        /// <summary>
        /// Refreshes certificates, if at least 1 hour passed sincle last refresh
        /// </summary>
        /// <param name="force">if true, will refresh certificate without checking elapsed time</param>
        public void RefreshCertificates(bool force);
    } // End Interface ICertificateService 


}

