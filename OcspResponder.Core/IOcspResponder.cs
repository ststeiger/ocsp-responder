

namespace OcspResponder.Core
{
    public interface IOcspResponder
    {
        System.Threading.Tasks.Task<OcspHttpResponse> Respond(OcspHttpRequest httpRequest);
    }
}