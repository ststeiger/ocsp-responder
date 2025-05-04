
namespace libCertificateService
{
    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection Connection { get; }
    }

}
