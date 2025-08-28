
namespace ReportServerProxyFF
{


    public class ReportProxyModule
        : System.Web.IHttpModule
    {

        static readonly string s_applicationRelativePath;
        static readonly int s_applicationRelativePathLength;

        static readonly string s_applicationBaseUrl;
        static readonly string s_reportServerDomain;
        static readonly string s_reportServerApplicationPath;
        static readonly string s_reportServerUrl;

        static ReportProxyModule()
        {
            s_applicationRelativePath = "/ReportServer";
            // s_applicationRelativePath = "/blabla";
            s_applicationRelativePathLength = s_applicationRelativePath.Length;

            string applicationVirtualPath = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
            if (applicationVirtualPath == null)
            {
                // Fallback or throw exception if required
                applicationVirtualPath = "/";
            }

            s_applicationBaseUrl = applicationVirtualPath.TrimEnd('/') + s_applicationRelativePath;

            s_reportServerDomain = "reportsrv2.cor-asp.ch".TrimEnd('/');
            s_reportServerApplicationPath = "/ReportServer";
            s_reportServerUrl = "https://" + s_reportServerDomain + s_reportServerApplicationPath;
        }


        public void Init(System.Web.HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
        }




        private void OnBeginRequest(object sender, System.EventArgs e)
        {
            System.Web.HttpApplication app = (System.Web.HttpApplication)sender;
            System.Web.HttpContext context = app.Context;

            string path = context.Request.Path;

            // Only proxy /ReportServer/*
            if (!path.StartsWith(s_applicationBaseUrl, System.StringComparison.OrdinalIgnoreCase))
                return;

            ProxyRequest(context);
        }

        private void ProxyRequest(System.Web.HttpContext context)
        {
            // Build target URL (strip local app path, keep /ReportServer/... intact)
            // string relativePath = context.Request.RawUrl;
            // string targetUrl = "https://example.com" + relativePath;

            // Build destination URL
            string catchAll = context.Request.RawUrl.Substring(
                context.Request.RawUrl.IndexOf(s_applicationRelativePath,
                System.StringComparison.OrdinalIgnoreCase)
                + s_applicationRelativePathLength
            );


            string applicationRelativePath = context.Request.RawUrl.Substring(
                0,
                context.Request.RawUrl.IndexOf(
                   s_applicationRelativePath,
                   System.StringComparison.OrdinalIgnoreCase
                )
               + s_applicationRelativePathLength
            );
            // System.Diagnostics.Debug.WriteLine(applicationRelativePath);




            string applicationCanonicalUrl = context.Request.Url.Scheme + System.Uri.SchemeDelimiter + context.Request.Url.Authority + applicationRelativePath;
            string applicationDomainWithVirtDir = context.Request.Url.Authority + applicationRelativePath;



            // System.Diagnostics.Debug.WriteLine(applicationCanonicalUrl);




            // Mispelled URL !
            // string targetUrl = "https://reportsrv2.cor-asp.ch/ReportSever" + catchAll;
            string targetUrl = s_reportServerUrl + catchAll;


            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(targetUrl);
            req.Method = context.Request.HttpMethod;
            req.AllowAutoRedirect = false;   // 🔹 don't follow 302/303 automatically


            // Add extra cookies if needed
            // req.CookieContainer.Add(new System.Net.Cookie("MyExtraCookie", "SomeValue", "/", "example.com"));


            System.Text.StringBuilder sbForwarded = new System.Text.StringBuilder();
            System.Text.StringBuilder sbIgnored = new System.Text.StringBuilder();


            // Forward headers
            foreach (string headerKey in context.Request.Headers)
            {
                if ("Accept".Equals(headerKey, System.StringComparison.OrdinalIgnoreCase))
                    req.Accept = context.Request.Headers[headerKey];
                else if ("User-Agent".Equals(headerKey, System.StringComparison.OrdinalIgnoreCase))
                    req.UserAgent = context.Request.Headers[headerKey];
                else if ("Accept-Encoding".Equals(headerKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    // continue; // gzip, deflate, br, zstd
                    req.AutomaticDecompression =
                        System.Net.DecompressionMethods.GZip |
                        System.Net.DecompressionMethods.Deflate;
                }
                else if ("Expect".Equals(headerKey, System.StringComparison.OrdinalIgnoreCase))
                {

                    if ("100-continue".Equals(context.Request.Headers[headerKey], System.StringComparison.OrdinalIgnoreCase))
                    {
                        req.ServicePoint.Expect100Continue = true;
                    }
                    else
                    {
                        req.ServicePoint.Expect100Continue = false;
                        req.Headers[headerKey] = context.Request.Headers[headerKey];
                    }

                    continue;
                }
                else if ("Set-Cookie".Equals(headerKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    string setCookieHeader = context.Request.Headers[headerKey];

                    if (!string.IsNullOrEmpty(setCookieHeader))
                    {
                        req.Headers[headerKey] = setCookieHeader;
                        System.Collections.Generic.List<Cookie> cookieList = CookieHelper.ParseSetCookieHeader(setCookieHeader);



                        System.Diagnostics.Debug.WriteLine(cookieList);
                    }


                    continue;
                }
                else if (System.Net.WebHeaderCollection.IsRestricted(headerKey))
                {
                    string skippedValue = context.Request.Headers[headerKey];
                    sbIgnored.Append(headerKey);
                    sbIgnored.Append(": ");
                    sbIgnored.AppendLine(skippedValue);
                    continue;
                }

                try
                {
                    req.Headers[headerKey] = context.Request.Headers[headerKey];

                    sbForwarded.Append(headerKey);
                    sbForwarded.Append(": ");
                    sbForwarded.AppendLine(context.Request.Headers[headerKey]);
                }
                catch (System.Exception setHeaderError)
                {
                    // fsck
                    sbIgnored.Append(" -- FAILED - ");
                    sbIgnored.Append(headerKey);
                    sbIgnored.Append(": ");
                    sbIgnored.Append(context.Request.Headers[headerKey]);
                    sbIgnored.Append(", Reason: ");
                    sbIgnored.AppendLine(setHeaderError.Message);
                }

            } // Next headerKey 

            string forwardedHeaders = sbForwarded.ToString();
            string ignoredHeaders = sbIgnored.ToString();
            System.Diagnostics.Debug.WriteLine(forwardedHeaders, ignoredHeaders);


            // Forward request body (POST, PUT, etc.)
            if (context.Request.InputStream != null && context.Request.InputStream.Length > 0)
            {
                req.ContentType = context.Request.ContentType;
                req.ContentLength = context.Request.InputStream.Length;

                using (System.IO.Stream reqStream = req.GetRequestStream())
                {
                    context.Request.InputStream.Position = 0;
                    context.Request.InputStream.CopyTo(reqStream);
                } // End Using reqStream 

            } // End Copy InputStream 


            try
            {
                using (System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)req.GetResponse())
                {
                    context.Response.StatusCode = (int)resp.StatusCode;

                    System.Text.StringBuilder sbProxiedHeaders = new System.Text.StringBuilder();


                    foreach (string headerKey in resp.Headers)
                    {
                        sbProxiedHeaders.Append(headerKey);
                        sbProxiedHeaders.Append(": ");
                        sbProxiedHeaders.AppendLine(resp.Headers[headerKey]);


                        if (headerKey.Equals("Content-Type", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string contentType = resp.Headers[headerKey];
                            context.Response.ContentType = contentType;
                            continue;
                        }
                        else if (headerKey.Equals("Set-Cookie", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string gottenCookies = resp.Headers[headerKey];

                            if (!string.IsNullOrEmpty(gottenCookies))
                            {
                                context.Response.Headers[headerKey] = gottenCookies;
                                System.Collections.Generic.List<Cookie> cookieList = CookieHelper.ParseSetCookieHeader(gottenCookies);

                                foreach (Cookie thisCookie in cookieList)
                                {
                                    System.Console.WriteLine(thisCookie.Domain);
                                    System.Console.WriteLine(thisCookie.Path);
                                }

                                System.Diagnostics.Debug.WriteLine(cookieList);
                            }
                            
                            continue;
                        }
                        else if (headerKey.Equals("Location", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string location = resp.Headers[headerKey];
                            // location = location.Replace("https://reportsrv2.cor-asp.ch/ReportServer", "https://localhost:44318/ReportServer");
                            location = location.Replace(s_reportServerUrl, applicationCanonicalUrl);

                            //if (location.StartsWith(s_reportServerApplicationPath))
                            //{
                            //    location = location.Substring(s_reportServerApplicationPath.Length);
                            //    location = s_applicationRelativePath + location;
                            //}

                            location = location.Replace(s_reportServerApplicationPath, s_applicationRelativePath);

                            context.Response.Headers[headerKey] = location;
                            continue;
                        }
                        else if (headerKey.Equals("Content-Encoding", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string whatWeThrowAway = resp.Headers[headerKey];
                            if (!string.IsNullOrEmpty(whatWeThrowAway))
                                System.Diagnostics.Debug.WriteLine(whatWeThrowAway);

                            continue;
                        }
                        else if (System.Net.WebHeaderCollection.IsRestricted(headerKey))
                        {
                            string restrictedValue = resp.Headers[headerKey];

                            System.Diagnostics.Debug.WriteLine("skipping " + headerKey);
                            System.Diagnostics.Debug.WriteLine("with value " + restrictedValue);

                            continue;
                        }

                        try
                        {
                            context.Response.Headers[headerKey] = resp.Headers[headerKey];
                        }
                        catch (System.Exception setHeaderError)
                        {
                            string restrictedValue = resp.Headers[headerKey];

                            System.Diagnostics.Debug.WriteLine("failed " + headerKey);

                            System.Diagnostics.Debug.WriteLine("with value " + restrictedValue);
                            System.Diagnostics.Debug.WriteLine("Reason: " + setHeaderError.Message);
                        } // End Catch 

                    } // Next headerKey 

                    string proxyHeaders = sbProxiedHeaders.ToString();
                    System.Diagnostics.Debug.WriteLine(proxyHeaders);

                    System.Diagnostics.Debug.WriteLine(context.Response.ContentType);

                    using (System.IO.Stream respStream = resp.GetResponseStream())
                    {
#if false
                        // Copy body
                        respStream.CopyTo(context.Response.OutputStream);
#elif false

                        using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream))
                        {
                            string responseText = reader.ReadToEnd();
                            // Optionally log it, inspect it, or modify it here
                            context.Response.Write(responseText);
                        }

#else
                        using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
                        {
                            respStream.CopyTo(memoryStream);
                            byte[] responseBytes = memoryStream.ToArray();

                            // Log/inspect response as UTF-8 string (safely)

                            if (context.Response.ContentType.TrimStart().StartsWith("text/html", System.StringComparison.InvariantCultureIgnoreCase))
                            {
                                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                                // Log it if you want
                                System.Diagnostics.Debug.WriteLine(responseText);

                                // responseText = responseText.Replace("href=\"/ReportServer", "href=\"/blabla");
                                // responseText = responseText.Replace("src=\"/ReportServer", "src=\"/blabla");

                                responseText = responseText.Replace("href=\"" + s_reportServerApplicationPath, "href=\"" + s_applicationRelativePath);
                                responseText = responseText.Replace("src=\"" + s_reportServerApplicationPath, "src=\"" + s_applicationRelativePath);
                                

                                responseText = responseText.Replace(s_reportServerDomain + "/ReportServer", applicationDomainWithVirtDir);


                                context.Response.Write(responseText);
                            }
                            else if (context.Response.ContentType.TrimStart().StartsWith("text/plain", System.StringComparison.InvariantCultureIgnoreCase))
                            {
                                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                                // Log it if you want
                                System.Diagnostics.Debug.WriteLine(responseText);


                                /*
                                <configuration>
                                  <system.web>
                                    <pages enableViewStateMac="false" />
                                  </system.web>
                                </configuration>
                                */


                                // responseText = responseText.Replace("href=\"/ReportServer", "href=\"/blabla");
                                // responseText = responseText.Replace("src=\"/ReportServer", "src=\"/blabla");

                                // responseText = responseText.Replace("href=\"" + s_reportServerApplicationPath, "href=\"" + s_applicationRelativePath);
                                // responseText = responseText.Replace("src=\"" + s_reportServerApplicationPath, "src=\"" + s_applicationRelativePath);


                                // responseText = responseText.Replace("image:url(" + s_reportServerApplicationPath, "image:url(" + s_applicationRelativePath);


                                // responseText = responseText.Replace(s_reportServerApplicationPath, s_applicationRelativePath);

                                context.Response.Write(responseText);
                            }
                            else if (context.Response.ContentType.TrimStart().StartsWith("text/xml", System.StringComparison.InvariantCultureIgnoreCase))
                            {
                                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                                // Log it if you want

                                string beautified = BeautifyXml(responseText);
                                System.Diagnostics.Debug.WriteLine(responseText);
                                System.Diagnostics.Debug.WriteLine(beautified);
                                



                                // responseText = responseText.Replace("href=\"/ReportServer", "href=\"/blabla");
                                // responseText = responseText.Replace("src=\"/ReportServer", "src=\"/blabla");

                                //responseText = responseText.Replace("href=\"" + s_reportServerApplicationPath, "href=\"" + s_applicationRelativePath);
                                //responseText = responseText.Replace("src=\"" + s_reportServerApplicationPath, "src=\"" + s_applicationRelativePath);


                                //responseText = responseText.Replace(s_reportServerDomain + "/ReportServer", applicationDomainWithVirtDir);


                                context.Response.Write(responseText);
                            }
                            else
                            {
                                // Write original bytes to response (preserves images, etc.)
                                context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                            }



                                
                        }
#endif
                    }
                }
            }
            catch (System.Net.WebException ex)
            {

                if (ex.Response is System.Net.HttpWebResponse errorResp)
                {
                    context.Response.TrySkipIisCustomErrors = true;

                    context.Response.StatusCode = (int)errorResp.StatusCode;

                    foreach (string headerKey in errorResp.Headers)
                    {
                        if (headerKey.Equals("content-type", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string contentType = errorResp.Headers[headerKey];
                            context.Response.ContentType = contentType;
                        }
                        else if (System.Net.WebHeaderCollection.IsRestricted(headerKey))
                            continue;
                        else if (headerKey.Equals("Set-Cookie", System.StringComparison.OrdinalIgnoreCase))
                        {
                            string errorCookies = errorResp.Headers[headerKey];

                            if (!string.IsNullOrEmpty(errorCookies))
                            {
                                context.Response.Headers[headerKey] = errorCookies;

                                System.Collections.Generic.List<Cookie> cookieList = CookieHelper.ParseSetCookieHeader(errorCookies);


                                System.Diagnostics.Debug.WriteLine(cookieList);
                            }
                            
                            continue;
                        }

                        try
                        {
                            context.Response.Headers[headerKey] = errorResp.Headers[headerKey];
                        }
                        catch (System.Exception setHeaderError)
                        {
                            string restrictedValue = errorResp.Headers[headerKey];

                            System.Diagnostics.Debug.WriteLine("failed " + headerKey);
                            
                            System.Diagnostics.Debug.WriteLine("with value " + restrictedValue);
                            System.Diagnostics.Debug.WriteLine("Reason: " + setHeaderError.Message);
                        } // ENd Catch 
                        

                    } // Next headerKey 



                    using (System.IO.Stream respStream = errorResp.GetResponseStream())
                    {


#if true
                        respStream.CopyTo(context.Response.OutputStream);
#else

                        using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream))
                        {
                            string responseText = reader.ReadToEnd();
                            // Optionally log it, inspect it, or modify it here
                            context.Response.Write(responseText);
                        }
#endif

                    } // End Using respStream 
                }
                else
                {
                    context.Response.StatusCode = 500;
                    context.Response.Write("Proxy error: " + ex.Message);
                }
            } // End Catch 

            // End request so no further processing happens
            context.ApplicationInstance.CompleteRequest();
        } // End Sub ProxyRequest 


        public static string BeautifyXml(string responseXml)
        {
            string resp = null;

            // Beautify XML for logging
            try
            {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml(responseXml);

                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",  // two spaces
                    NewLineChars = "\r\n",
                    NewLineHandling = System.Xml.NewLineHandling.Replace,
                    Encoding = System.Text.Encoding.UTF8
                };



                using (System.IO.StringWriter sw = new System.IO.StringWriter())
                {

                    using (ForwardingTextWriterWithEncoding fwriter = new ForwardingTextWriterWithEncoding(
                        sw, settings.Encoding)
                    )
                    {
                        using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(fwriter, settings))
                        {
                            doc.Save(writer);
                            writer.Flush();
                            fwriter.Flush();
                            sw.Flush();
                            resp = sw.ToString();
                        } // End Using writer 

                    } // End Using fwriter 

                } // End Using sw 
                
                System.Diagnostics.Debug.WriteLine(resp); // log formatted XML
            } // End Try 
            catch (System.Xml.XmlException xe)
            {
                // fallback to raw text if it's not well-formed XML
                System.Diagnostics.Debug.WriteLine(xe.Message);
            } // End Catch 

            return resp;
        } // End Function BeautifyXml 


        public void Dispose()
        { } // End Sub Dispose 


    } // End Class 


} // End Namespace 
