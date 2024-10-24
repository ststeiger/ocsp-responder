
namespace OcspResponder.Core
{
    public class OcspHttpResponse
    {
        public byte[] Content { get; }

        public string MediaType { get; }

        public System.Net.HttpStatusCode Status { get; }

        public OcspHttpResponse(byte[] content, string mediaType, System.Net.HttpStatusCode status)
        {
            Content = content;
            MediaType = mediaType;
            Status = status;
        }
    }
}