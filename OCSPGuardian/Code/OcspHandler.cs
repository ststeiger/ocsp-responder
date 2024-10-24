

namespace OCSPGuardian
{

    using OcspResponder.AspNetCore; // for ToOcspHttpRequest 


    public class OcspHandler 
    {


        public static async System.Threading.Tasks.Task<global::OcspResponder.AspNetCore.MinimalOcspResult> HandleGet(
            string encoded, 
            Microsoft.AspNetCore.Http.HttpRequest request, 
            global::OcspResponder.Core.IOcspResponder ocspResponder
        ) 
        {
            global::OcspResponder.Core.OcspHttpRequest ocspHttpRequest = await request.ToOcspHttpRequest();
            global::OcspResponder.Core.OcspHttpResponse ocspHttpResponse = await ocspResponder.Respond(ocspHttpRequest);
            
            // new OcspResponder.Responder.Services.RequestMetadata(request.Connection.RemoteIpAddress);
            return new global::OcspResponder.AspNetCore.MinimalOcspResult(ocspHttpResponse);
        } // End Task HandleGet 


        public static async System.Threading.Tasks.Task<global::OcspResponder.AspNetCore.MinimalOcspResult> HandlePost(
            Microsoft.AspNetCore.Http.HttpRequest request, 
            global::OcspResponder.Core.IOcspResponder ocspResponder
        )
        {
            global::OcspResponder.Core.OcspHttpRequest ocspHttpRequest = await request.ToOcspHttpRequest();
            global::OcspResponder.Core.OcspHttpResponse ocspHttpResponse = await ocspResponder.Respond(ocspHttpRequest);

            return new global::OcspResponder.AspNetCore.MinimalOcspResult(ocspHttpResponse);
        } // End Task HandlePost 


    } // End Class OcspHandler 
}
