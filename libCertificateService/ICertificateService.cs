
namespace libCertificateService
{


    public interface ICertificateService
    {
        System.Threading.Tasks.Task<Certificate> GetCertificateAsync(string domainName);
        System.Security.Cryptography.X509Certificates.X509Certificate2 SelectCertificate(string domainName);
        
        System.Threading.Tasks.Task RefreshCertificates();
    } // End Interface ICertificateService
    
    
} // End Namespace 

