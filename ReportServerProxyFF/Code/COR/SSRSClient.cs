
namespace ReportServerProxyFF
{


    public class SSRSClient
    {
  

        public static string[] PostToSSRS(string ssrsLink, string ssrsData)
        {
            // System.Net.CookieContainer _cookieContainer = new System.Net.CookieContainer();

            string url = ssrsLink.TrimEnd('/') + "/logon.aspx";

            // Prepare POST data
            string postData = string.Format("data={0}&SSO=FMS", System.Uri.EscapeDataString(ssrsData));
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);

            // Create the web request
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            // request.CookieContainer = _cookieContainer;
            request.AllowAutoRedirect = false; // Capture 302 with Set-Cookie

            // Write POST data to request stream
            using (System.IO.Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // Get the response
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();

            // Check for successful status code or 302 redirect
            if (response.StatusCode != System.Net.HttpStatusCode.OK && response.StatusCode != System.Net.HttpStatusCode.Found)
            {
                throw new System.Exception("SSRS login failed with status code: " + response.StatusCode);
            }

            /*
            // Extract cookies from response
            System.Collections.Generic.List<string> cookies = new System.Collections.Generic.List<string>();
            foreach (Cookie cookie in response.Cookies)
            {
                cookies.Add(cookie.Name + "=" + cookie.Value);
            }

            response.Close();
            return cookies.ToArray();
            */

            System.Collections.Generic.List<string> cookies = new System.Collections.Generic.List<string>();
            foreach (string headerKey in response.Headers.AllKeys)
            {
                if (string.Equals(headerKey, "Set-Cookie", System.StringComparison.OrdinalIgnoreCase))
                {
                    string[] headerValues = response.Headers.GetValues(headerKey);
                    if (headerValues != null)
                    {
                        cookies.AddRange(headerValues);
                    }
                } // End if 

            } // Next headerKey 

            return cookies.ToArray();
        } // End Function PostToSSRS 


    } // End Class SSRSClient 


} // End Namespace ReportServerProxyFF 
