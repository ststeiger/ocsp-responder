
namespace OCSPGuardian.Pages
{


    public class RequestInfoModel
        : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public System.Collections.Generic.Dictionary<string, string> Headers { get; set; } =
            new System.Collections.Generic.Dictionary<string, string>();
        public System.Collections.Generic.Dictionary<string, string> PostData { get; set; } =
            new System.Collections.Generic.Dictionary<string, string>();
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> Cookies { get; set; } =
            new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();



        public readonly bool WITH_INSECURE_DATA = false;

        public ProxyProtocol.IProxyProtocolFeature? ProxyProtocolFeature { get; set; }
        public bool ShowProxyInfo { get; set; } = false;
        public string ClientIP { get; set; }
        public string ClientIpForwarded { get; set; }



        public async System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnGetAsync()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers)
            {
                if (!WITH_INSECURE_DATA && System.StringComparer.InvariantCultureIgnoreCase.Equals(header.Key, "cookie"))
                    continue;

                if (!WITH_INSECURE_DATA && System.StringComparer.InvariantCultureIgnoreCase.Equals(header.Key, "X-Original-For"))
                    continue;

                Headers[header.Key] = header.Value.ToString();
            } // Next header 

            if (WITH_INSECURE_DATA)
            {
                
                foreach (System.Collections.Generic.KeyValuePair<string, string> cookie in Request.Cookies)
                {
                    Cookies.Add(cookie);
                } // Next cookie 

            } // End if (WITH_INSECURE_DATA) 


            ProxyProtocolFeature = HttpContext.Features.Get<ProxyProtocol.IProxyProtocolFeature>(); 

            System.Net.IPAddress ip = this.Request.HttpContext.Connection.RemoteIpAddress.IsIPv4MappedToIPv6 ?
                this.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4() : this.Request.HttpContext.Connection.RemoteIpAddress;

            this.ClientIP = ip.ToString();
            this.ClientIpForwarded = this.Request.HttpContext.Request.Headers["X-Forwarded-For"].ToString() ?? "EMPTY" ;

            return Page();
        } // End Task OnGetAsync 


        public async System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostAsync()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in Request.Headers)
            {
                if (!WITH_INSECURE_DATA && System.StringComparer.InvariantCultureIgnoreCase.Equals(header.Key, "cookie"))
                    continue;

                if (!WITH_INSECURE_DATA && System.StringComparer.InvariantCultureIgnoreCase.Equals(header.Key, "X-Original-For"))
                    continue;

                Headers[header.Key] = header.Value.ToString();
            } // Next header 

            // Get form data
            if (Request.HasFormContentType)
            {
                Microsoft.AspNetCore.Http.IFormCollection form = await Request.ReadFormAsync();
                foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> field in form)
                {
                    // Note: do not show csrf-token 
                    PostData[field.Key] = field.Value.ToString();
                } // Next field

            } // End if form-post 

            if (WITH_INSECURE_DATA)
            {
                
                foreach (System.Collections.Generic.KeyValuePair<string, string> cookie in Request.Cookies)
                {
                    Cookies.Add(cookie);
                } // Next cookie 

            } // End if (WITH_INSECURE_DATA) 

            ProxyProtocolFeature = HttpContext.Features.Get<ProxyProtocol.IProxyProtocolFeature>();

            System.Net.IPAddress ip = this.Request.HttpContext.Connection.RemoteIpAddress.IsIPv4MappedToIPv6 ?
                this.Request.HttpContext.Connection.RemoteIpAddress.MapToIPv4() : this.Request.HttpContext.Connection.RemoteIpAddress;

            this.ClientIP = ip.ToString();
            this.ClientIpForwarded = this.Request.HttpContext.Request.Headers["X-Forwarded-For"].ToString(); ;

            return Page();
        } // End Task OnPostAsync 


    } // End Class RequestInfoModel 


} // End Namespace 
