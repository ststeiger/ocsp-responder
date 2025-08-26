
namespace libCertificateService
{
    public interface ICertificateRepository
    {
        System.Threading.Tasks.Task<Certificate?> GetLatestValidCertificateForDomain(string domain);
        System.Threading.Tasks.Task<System.Collections.Generic.List<Certificate>> GetAllValidCertificates();

    }

    public interface ICertificateService
    {
        System.Threading.Tasks.Task<Certificate> GetCertificateAsync(string domainName);
        System.Security.Cryptography.X509Certificates.X509Certificate2 GetCertificate2(string domainName);

        System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> 
            GetCertificate2Async(string domainName);

        System.Threading.Tasks.Task RefreshCertificates();
    }
}

