
namespace OcspResponder.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="OcspHttpResponse"/>
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Converts the <see cref="OcspHttpResponse"/> to <see cref="HttpResponseMessage"/>
        /// </summary>
        /// <param name="ocspHttpResponse"><see cref="OcspHttpResponse"/></param>
        /// <returns><see cref="HttpResponseMessage"/></returns>
        public static System.Net.Http.HttpResponseMessage ToHttpResponseMessage(this OcspHttpResponse ocspHttpResponse)
        {
            var httpResponseMessage = new System.Net.Http.HttpResponseMessage(ocspHttpResponse.Status)
            {
                Content = new System.Net.Http.ByteArrayContent(ocspHttpResponse.Content)
            };

            httpResponseMessage.Content.Headers.ContentType.MediaType = ocspHttpResponse.MediaType;
            return httpResponseMessage;
        }
    }
}