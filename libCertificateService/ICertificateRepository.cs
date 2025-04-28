
namespace libCertificateService
{
    public interface ICertificateRepository
    {
        System.Threading.Tasks.Task<System.Collections.Generic.List<Certificate>> GetAllValidCertificates();

        // if not found in cache, search in DB
        System.Threading.Tasks.Task<Certificate> GetLatestValidCertificateForDomain(string domainName);
    }

}
