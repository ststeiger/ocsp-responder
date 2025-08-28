
namespace ReportServerProxyFF
{
    public class ReportProxyHandler
        : System.Web.IHttpHandler
    {


        public void ProcessRequest(
            System.Web.HttpContext context
        )
        {
            // Build destination URL
            string catchAll = context.Request.RawUrl.Substring(
                context.Request.RawUrl.IndexOf("/ReportServer",
                System.StringComparison.OrdinalIgnoreCase)
            );

            string targetUrl = "https://reportsrv2.cor-asp.ch/ReportServer" + catchAll;

            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(targetUrl);
            req.Method = context.Request.HttpMethod;

            req.CookieContainer = new System.Net.CookieContainer();
            // Forward client cookies
            foreach (string cookieKey in context.Request.Cookies)
            {
                System.Web.HttpCookie clientCookie = context.Request.Cookies[cookieKey];
                if (clientCookie != null)
                {
                    System.Net.Cookie forwarded = new System.Net.Cookie(clientCookie.Name, clientCookie.Value, clientCookie.Path, "example.com");
                    req.CookieContainer.Add(forwarded);
                }
            }

            // 🔹 Add your own cookies (for auth/session etc.)
            req.CookieContainer.Add(new System.Net.Cookie("MyExtraCookie", "SomeValue", "/", "example.com"));



            // Copy headers
            foreach (string headerKey in context.Request.Headers)
            {
                if (!System.Net.WebHeaderCollection.IsRestricted(headerKey))
                {
                    try
                    {
                        req.Headers[headerKey] = context.Request.Headers[headerKey];
                    }
                    catch { /* Some headers (like Host) cannot be set */ }
                }
            }

            // Copy request body if present
            if (context.Request.InputStream != null && context.Request.InputStream.Length > 0)
            {
                using (System.IO.Stream reqStream = req.GetRequestStream())
                {
                    context.Request.InputStream.CopyTo(reqStream);
                }
            }

            // Get response
            using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
            {
                context.Response.StatusCode = (int)resp.StatusCode;

                // Copy response headers
                foreach (string headerKey in resp.Headers)
                {
                    context.Response.Headers[headerKey] = resp.Headers[headerKey];
                }

                // Copy response body
                using (System.IO.Stream respStream = resp.GetResponseStream())
                {
                    respStream.CopyTo(context.Response.OutputStream);
                }
            }
        }


        public bool IsReusable 
        { 
            get 
            { 
                return false; 
            } 
        }


    }


}
