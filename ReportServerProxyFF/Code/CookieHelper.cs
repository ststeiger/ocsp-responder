
namespace ReportServerProxyFF
{


    [System.Diagnostics.DebuggerDisplay("{this.Name} = {this.Value}")]
    public class Cookie
    {

        public bool IsSetCookie; // Is it from Set-Cookie header, or from Cookie-Header 


        /// <summary>
        /// Case-sensitive
        /// </summary>
        public string Name;
        public string Value;

        /// <summary>
        /// The Path attribute is case-SENSITIVE 
        /// ✅ If Path is Not Set: The browser uses the path of the request URL that set the cookie as the default.
        /// Example: If a response from https://example.com/account/login sets a cookie without a Path, the browser uses /account/ as the path.
        /// ✅ If Path Is Set: The browser will send the cookie only for requests to that path and any sub-paths.
        /// </summary>
        public string Path;


        /// <summary>
        /// The Domain attribute can be set in the Set-Cookie header, but it's optional. 
        /// It is NOT case-sensitive. 
        /// If the Domain attribute is omitted: The cookie is only sent to the exact host that set it(host-only cookie).
        /// If the Domain attribute is set: The cookie is shared with the specified domain and all of its subdomains.
        /// e.g. Set-Cookie: sessionId=abc123; Domain=example.com; Path=/; Secure; HttpOnly 
        /// then the cookie is sent to example.com and all its subdomains, like www.example.com, app.example.com, etc.
        /// Set-Cookie: sessionId=abc123; Path=/; Secure; HttpOnly
        /// This cookie is only sent back to the exact domain that set it — e.g., www.example.com, but not sub.example.com.
        /// </summary>
        public string Domain;
        public System.DateTime? Expires;
        public bool? Secure;
        public bool? HttpOnly;

        public string SameSite; // can be "Strict", "Lax", "None", or null
        public int? MaxAge;          // in seconds
        public bool? Partitioned;    // true/false


        

        public Cookie()
        {
            // All nullable fields default to null
            Path = null;
            Domain = null;
            Expires = null;
            Secure = null;
            HttpOnly = null;
            SameSite = null;
        } // End Constructor 


        public override string ToString()
        {
            string ret = CookieHelper.BuildSetCookieHeader(this);
            return ret;
        } // End Function ToString 


        public string ToSetCookieHeader()
        {
            return CookieHelper.BuildSetCookieHeader(this);
        } // End Function ToSetCookieHeader 


    } // End Class Cookie 


    public static class CookieHelper
    {


        // Builds the Cookie header string from a list of Cookie objects
        // Note: this is the Cookie-Header, not the Set-Cookie header 
        public static string BuildCookieHeaderForCookies(System.Collections.Generic.List<Cookie> cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < cookies.Count; i++)
            {
                Cookie cookie = cookies[i];

                if (cookie == null || string.IsNullOrEmpty(cookie.Name))
                    continue;

                if (sb.Length > 0)
                    sb.Append("; ");

                sb.Append(cookie.Name);
                sb.Append("=");
                if(cookie.Value != null)
                    sb.Append(cookie.Value);
            } // Next i 

            return sb.ToString();
        } // End Function BuildCookieHeaderForCookies 




        /// <summary>
        /// Converts a Cookie object into the corresponding Set-Cookie header string.
        /// Only attributes that are non-null are included.
        /// </summary>
        public static string BuildSetCookieHeader(Cookie c)
        {
            if (string.IsNullOrEmpty(c.Name))
                return null; // skip invalid cookies

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Name=Value
            sb.Append(c.Name);
            sb.Append('=');
            sb.Append(c.Value ?? string.Empty);


            if (c.Expires.HasValue)
            {
                sb.Append("; Expires=");
                sb.Append(c.Expires.Value.ToUniversalTime().ToString("R", System.Globalization.CultureInfo.InvariantCulture)); // RFC1123 format
            }

            // Optional attributes
            if (!string.IsNullOrEmpty(c.Path))
            {
                sb.Append("; Path=");
                sb.Append(c.Path);
            }

            if (!string.IsNullOrEmpty(c.Domain))
            {
                sb.Append("; Domain=");
                sb.Append(c.Domain);
            }

            if (c.Secure.HasValue && c.Secure.Value)
            {
                sb.Append("; Secure");
            }

            if (c.HttpOnly.HasValue && c.HttpOnly.Value)
            {
                sb.Append("; HttpOnly");
            }

            if (c.MaxAge.HasValue)
            {
                sb.Append("; Max-Age=");
                sb.Append(c.MaxAge.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(c.SameSite))
            {
                sb.Append("; SameSite=");
                sb.Append(c.SameSite);
            }

            if (c.Partitioned.HasValue && c.Partitioned.Value)
            {
                sb.Append("; Partitioned");
            }

            string ret = sb.ToString();
            sb.Clear();
            sb = null;

            return ret;
        } // End Function BuildSetCookieHeader 


        // Parses the Cookie-Header string into a list of Cookie objects 
        // Note: this is the Cookie-Header, not the Set-Cookie header 
        public static System.Collections.Generic.List<Cookie> ParseCookieHeader(string cookieHeader)
        {
            System.Collections.Generic.List<Cookie> cookies = new System.Collections.Generic.List<Cookie>();

            if (string.IsNullOrEmpty(cookieHeader))
                return cookies;

            int length = cookieHeader.Length;
            int i = 0;

            while (i < length)
            {
                // Skip any whitespace or semicolon
                while (i < length && (cookieHeader[i] == ' ' || cookieHeader[i] == ';'))
                    i++;

                if (i >= length)
                    break;

                // Extract name
                int nameStart = i;
                while (i < length && cookieHeader[i] != '=')
                    i++;

                if (i >= length)
                    break;

                string name = cookieHeader.Substring(nameStart, i - nameStart).Trim();

                i++; // skip '='

                // Extract value
                int valueStart = i;
                while (i < length && cookieHeader[i] != ';')
                    i++;

                string value = cookieHeader.Substring(valueStart, i - valueStart).Trim();

                // Create and add Cookie object
                Cookie cookie = new Cookie();
                cookie.Name = name;
                cookie.Value = value;

                // All other fields remain null
                cookies.Add(cookie);
            } // Whend 

            return cookies;
        } // End Function ParseCookieHeader 


        // Parses the Set-Cookie-Header string into a list of Cookie objects 
        // Note: this is the Set-Cookie-Header, not the Cookie header 
        public static System.Collections.Generic.List<Cookie> ParseSetCookieHeader(string cookieHeader)
        {
            System.Collections.Generic.List<Cookie> cookieList = new System.Collections.Generic.List<Cookie>();

            if (string.IsNullOrEmpty(cookieHeader))
                return cookieList;

            System.Collections.Generic.List<string> cookieStrings = SplitSetCookieHeader(cookieHeader);

            for (int i = 0; i < cookieStrings.Count; i++)
            {
                string cookieString = cookieStrings[i];
                Cookie cookie = ParseSingleCookie(cookieString);
                
                if (cookie != null)
                {
                    cookie.IsSetCookie = true;
                    cookieList.Add(cookie);
                }
                    
            } // Next i 

            return cookieList;
        } // End Function ParseSetCookieHeader 


        // Splits a Set-Cookie header into individual cookie strings, respecting commas in expires
        private static System.Collections.Generic.List<string> SplitSetCookieHeader(string cookieHeader)
        {
            System.Collections.Generic.List<string> cookieStrings =
                new System.Collections.Generic.List<string>();

            cookieHeader = cookieHeader.Replace("\r", "").Replace("\n", "");

            string[] parts = cookieHeader.Split(',');
            int i = 0;
            while (i < parts.Length)
            {
                string part = parts[i];

                // Check for Expires= which contains a comma
                if (part.IndexOf("expires=", System.StringComparison.OrdinalIgnoreCase) > 0 && (i + 1) < parts.Length)
                {
                    part = part + "," + parts[i + 1];
                    i = i + 1; // skip next part since we combined it
                } // End if expires 

                cookieStrings.Add(part.Trim());
                i = i + 1;
            } // Whend 

            return cookieStrings;
        } // End Function SplitSetCookieHeader 


        // Parses a single cookie string into a Cookie object
        private static Cookie ParseSingleCookie(string cookieString)
        {
            if (string.IsNullOrEmpty(cookieString))
                return null;

            string[] segments = cookieString.Split(';');
            if (segments.Length == 0)
                return null;

            Cookie cookie = new Cookie();

            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i].Trim();

                if (i == 0)
                {
                    // First segment is Name=Value
                    int eq = segment.IndexOf('=');
                    if (eq > 0)
                    {
                        cookie.Name = segment.Substring(0, eq).Trim();
                        cookie.Value = segment.Substring(eq + 1).Trim();
                    }
                    else
                    {
                        // No value? treat entire segment as Name
                        cookie.Name = segment;
                        cookie.Value = string.Empty;
                    }
                }
                else
                {
                    // Subsequent segments are optional attributes
                    int eq = segment.IndexOf('=');
                    string attrName = eq > 0 ? segment.Substring(0, eq).Trim().ToLowerInvariant() : segment.ToLowerInvariant();
                    string attrValue = eq > 0 ? segment.Substring(eq + 1).Trim() : null;

                    switch (attrName)
                    {
                        case "path":
                            if (!string.IsNullOrEmpty(attrValue))
                                cookie.Path = attrValue;
                            break;
                        case "domain":
                            if (!string.IsNullOrEmpty(attrValue))
                                cookie.Domain = attrValue;
                            break;
                        case "expires":
                            System.DateTime dt;
                            if (System.DateTime.TryParse(attrValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out dt))
                                cookie.Expires = dt;
                            break;
                        case "secure":
                            cookie.Secure = true;
                            break;
                        case "httponly":
                            cookie.HttpOnly = true;
                            break;
                        case "max-age":
                            int seconds;
                            if (int.TryParse(attrValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                                cookie.MaxAge = seconds;
                            break;
                        case "samesite":
                            if (!string.IsNullOrEmpty(attrValue))
                                cookie.SameSite = attrValue; // store as-is
                            break;
                        case "partitioned":
                            cookie.Partitioned = true; // no value needed
                            break;
                    } // End Switch 

                } // End Else 

            } // Next i (segments[i])

            return cookie;
        } // End Function ParseSingleCookie 


    } // End Class CookieHelper 


} // End Namespace 
