
namespace OcspResponder.AspNetCore
{


    public class MinimalOcspActionResult
        : Microsoft.AspNetCore.Http.IResult
    {

        /// <summary>
        /// A <see cref="OcspHttpResponse"/> from OcspResponder.Core
        /// </summary>
        private OcspResponder.Core.OcspHttpResponse OcspHttpResponse { get; }



        /// <summary>
        /// Creates a <see cref="IActionResult"/> for Ocsp responses
        /// </summary>
        /// <param name="ocspHttpResponse"><see cref="OcspHttpResponse"/></param>
        public MinimalOcspActionResult(OcspResponder.Core.OcspHttpResponse ocspHttpResponse)
        {
            OcspHttpResponse = ocspHttpResponse;
        } // End Constructor 


        async System.Threading.Tasks.Task Microsoft.AspNetCore.Http.IResult.ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = OcspHttpResponse.MediaType;
            await context.Response.BodyWriter.WriteAsync(OcspHttpResponse.Content);
        }
    }


}
