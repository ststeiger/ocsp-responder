
namespace libCertificateService
{
    public class Certificate
    {
        public string? DomainName { get; set; }
        public byte[]? PfxData { get; set; }
        public System.DateTime? ValidFrom { get; set; }
        public System.DateTime? ValidUntil { get; set; }
        public System.DateTime? CreatedAt { get; set; }
    }
}
