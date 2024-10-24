
namespace OcspResponder.AspNetCore
{


    /// <summary>
    /// Set of extension methods for <see cref="HttpRequest"/>
    /// </summary>
    public static class HttpRequestExtensions
    {
        private const string UnknownHostName = "UNKNOWN-HOST";


        /// <summary>
        /// Converts <see cref="HttpRequest"/> to <see cref="OcspHttpRequest"/>
        /// </summary>
        /// <param name="request"><see cref="HttpRequest"/></param>
        /// <returns><see cref="OcspHttpRequest"/></returns>
        public static async System.Threading.Tasks.Task<OcspResponder.Core.OcspHttpRequest> ToOcspHttpRequest(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            Core.OcspHttpRequest ocspHttpRequest = new OcspResponder.Core.OcspHttpRequest();
            ocspHttpRequest.HttpMethod = request.Method;
            ocspHttpRequest.MediaType = request.ContentType;
            ocspHttpRequest.RequestUri = request.GetUri();
            ocspHttpRequest.Content = await request.GetRawBodyBytesAsync();

            return ocspHttpRequest;
        }

        /// <summary>
        /// Gets http request Uri from request object
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <returns>A New Uri object representing request Uri</returns>
        private static System.Uri GetUri(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            if (null == request)
            {
                throw new System.ArgumentNullException("request");
            }

            if (true == string.IsNullOrWhiteSpace(request.Scheme))
            {
                throw new System.ArgumentException("Http request Scheme is not specified");
            }

            string hostName = request.Host.HasValue ? request.Host.ToString() : UnknownHostName;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            builder.Append(request.Scheme)
                .Append("://")
                .Append(hostName);

            if (true == request.Path.HasValue)
            {
                builder.Append(request.Path.Value);
            }

            if (true == request.QueryString.HasValue)
            {
                builder.Append(request.QueryString);
            }

            return new System.Uri(builder.ToString());
        }


        /// <summary>
        /// Retrieves the raw body as a byte array from the Request.Body stream
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/></param>
        /// <returns></returns>
        private static async System.Threading.Tasks.Task<byte[]> GetRawBodyBytesAsync(this Microsoft.AspNetCore.Http.HttpRequest request)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(2048))
            {
                await request.Body.CopyToAsync(ms);
                return ms.ToArray();
            }
        }


    }


}