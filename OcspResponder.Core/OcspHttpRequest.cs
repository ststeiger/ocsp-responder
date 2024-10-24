
namespace OcspResponder.Core
{

    public class OcspHttpRequest
    {
        public string HttpMethod { get; set; }

        public System.Uri RequestUri { get; set; }

        public string MediaType { get; set; }

        public byte[] Content { get; set; }
    }

}
