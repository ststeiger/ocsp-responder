
namespace OcspResponder.AspNetCore
{


    /// <summary>
    /// An Ocsp response
    /// </summary>
    public class OcspActionResult 
        : Microsoft.AspNetCore.Mvc.IActionResult
    {


        /// <summary>
        /// A <see cref="OcspHttpResponse"/> from OcspResponder.Core
        /// </summary>
        private OcspResponder.Core.OcspHttpResponse OcspHttpResponse { get; }


        /// <summary>
        /// Creates a <see cref="IActionResult"/> for Ocsp responses
        /// </summary>
        /// <param name="ocspHttpResponse"><see cref="OcspHttpResponse"/></param>
        public OcspActionResult(OcspResponder.Core.OcspHttpResponse ocspHttpResponse)
        {
            OcspHttpResponse = ocspHttpResponse;
        } // End Constructor 


        /// <inheritdoc />
        public async System.Threading.Tasks.Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
        {
            Microsoft.AspNetCore.Mvc.FileContentResult contentResult = 
                new Microsoft.AspNetCore.Mvc.FileContentResult(
                    OcspHttpResponse.Content, 
                    OcspHttpResponse.MediaType
            );

            await contentResult.ExecuteResultAsync(context);
        } // End Task ExecuteResultAsync 


    } // End Class OcspActionResult 


} // End Namespace 
