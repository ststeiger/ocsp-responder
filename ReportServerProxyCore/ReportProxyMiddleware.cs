
namespace ReportServerProxyCore
{

    using Microsoft.AspNetCore.Http;
    using System.Linq;


    public class ReportProxyMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private readonly System.Net.Http.HttpClient m_httpClient;

        private const string ReportPrefix = "/ReportServer";

        private static readonly string s_reportServerDomain = "reportsrv2.cor-asp.ch";
        private static readonly string s_reportServerApplicationPath = "/ReportServer";
        private static readonly string s_reportServerUrl = "https://" + s_reportServerDomain + s_reportServerApplicationPath;

        public ReportProxyMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            System.Net.Http.IHttpClientFactory httpClientFactory
        )
        {
            _next = next;
            // this.m_httpClient = httpClientFactory.CreateClient();
            this.m_httpClient = httpClientFactory.CreateClient("ReportProxy");

            //new System.Net.Http.HttpClientHandler
            //{
            //    // Turn it OFF if needed
            //    UseCookies = false,
            //    // Expect100Continue = false,
            //    AutomaticDecompression = System.Net.DecompressionMethods.GZip 
            //    | System.Net.DecompressionMethods.Deflate 
            //    | System.Net.DecompressionMethods.Brotli
            //};


        }

        public async System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context)
        {
            string? path = context.Request.Path.Value;

            if (path == null || !path.StartsWith(ReportPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            await ProxyRequest(context);
        }

        private async System.Threading.Tasks.Task ProxyRequest(Microsoft.AspNetCore.Http.HttpContext context)
        {

            try
            {
                Microsoft.AspNetCore.Http.HttpRequest request = context.Request;
                Microsoft.AspNetCore.Http.HttpResponse response = context.Response;

                string? catchAll = request.Path.Value?.Substring(ReportPrefix.Length) + request.QueryString;
                string targetUrl = s_reportServerUrl + catchAll;


                System.Net.Http.HttpRequestMessage targetRequest = new System.Net.Http.HttpRequestMessage(
                    new System.Net.Http.HttpMethod(request.Method),
                    targetUrl
                );

                // The User-Agent header is absolutely critical for ASP.NET AJAX controls and many web applications
                // they often do browser detection and serve different JavaScript/resources based on it.
                // When the User-Agent is missing or generic (like the default HttpClient user agent), the server might:

                // - Serve different JavaScript files
                // - Return different HTML markup
                // - Change script loading behavior
                // - Disable certain features

                string ua = request.Headers["User-Agent"].ToString();
                if(!string.IsNullOrEmpty(ua))
                targetRequest.Headers.UserAgent.ParseAdd(ua);



                // Copy request headers
                foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers)
                {
                    if (!System.Net.WebHeaderCollection.IsRestricted(header.Key) &&
                        !header.Key.StartsWith(":") &&
                        !string.Equals(header.Key, "Host", System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // // Copy body if needed
                //if (request.ContentLength > 0)
                //{
                //    request.EnableBuffering();
                //    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                //    {
                //        await request.Body.CopyToAsync(ms);
                //        ms.Position = 0;
                //        request.Body.Position = 0;
                //        targetRequest.Content = new System.Net.Http.StreamContent(ms);
                //        targetRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
                //    }

                //}


                // Send the request
                System.Net.Http.HttpResponseMessage targetResponse;




                // Copy body if needed
                //if (request.ContentLength > 0)
                //{
                //    request.EnableBuffering();
                /*
                if (request.HasFormContentType)
                {
                    IFormCollection form = await request.ReadFormAsync();


                    if (form.Count > 0)
                        System.Console.WriteLine("Good");
                    else 
                        System.Console.WriteLine("aaa");



                    if (request.ContentType != null && 
                        request.ContentType.StartsWith("multipart/form-data", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle file uploads
                        System.Net.Http.MultipartFormDataContent multipartContent = new System.Net.Http.MultipartFormDataContent();

                        foreach (System.Collections.Generic.KeyValuePair<
                            string, Microsoft.Extensions.Primitives.StringValues
                            > field in form)
                        {

                            // Microsoft.Extensions.Primitives.StringValues x = field.Value;
                            string value = field.Value.ToString();
                            multipartContent.Add(new System.Net.Http.StringContent(value ?? ""), field.Key);
                        }

                        foreach (IFormFile file in form.Files)
                        {
                            System.Net.Http.StreamContent streamContent = new System.Net.Http.StreamContent(file.OpenReadStream());
                            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                            multipartContent.Add(streamContent, file.Name, file.FileName);
                        }

                        targetRequest.Content = multipartContent;
                    }
                    else
                    {
                        // application/x-www-form-urlencoded
                        System.Collections.Generic.IEnumerable<
                             System.Collections.Generic.KeyValuePair<string, string?>
                             > keyValuePairs = form.Select(
                                 kvp => new System.Collections.Generic.KeyValuePair<string, string?>(kvp.Key, kvp.Value)
                        );

                        targetRequest.Content = new System.Net.Http.FormUrlEncodedContent(keyValuePairs);
                    }
                }
                else
                */
                {
                    // Fixed request body handling for .NET Core proxy middleware
                    if (request.ContentLength > 0 && request.Body.CanRead)
                    {
                        // Create a memory stream to buffer the request body
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            // Copy the request body to our buffer
                            await request.Body.CopyToAsync(ms);

                            // Get the buffered data
                            byte[] bodyBytes = ms.ToArray();

                            // Create content from the buffered data
                            System.Net.Http.ByteArrayContent content = new System.Net.Http.ByteArrayContent(bodyBytes);


                            // Set content type if available
                            if (!string.IsNullOrEmpty(request.ContentType))
                            {
                                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(request.ContentType);
                            }

                            // Content-Length is automatically set by ByteArrayContent
                            targetRequest.Content = content;
                        }
                    }

                    //// Not a form, just pass through raw stream
                    //long a = request.Body.Length;

                    //await request.Body.CopyToAsync(ms);
                    //ms.Position = 0;
                    //request.Body.Position = 0;

                    //System.Net.Http.StreamContent content = new System.Net.Http.StreamContent(ms);
                    //content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(request.ContentType ?? "application/octet-stream");
                    //content.Headers.ContentLength = a;


                    //// new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType ?? "application/octetstream");

                    //targetRequest.Content = content;

                    // }




                    try
                    {
                        targetResponse = await this.m_httpClient.SendAsync(targetRequest, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        response.StatusCode = 502;
                        await response.WriteAsync("Proxy Error: " + ex.Message);
                        return;
                    }
                }
                response.StatusCode = (int)targetResponse.StatusCode;

                // Copy response headers
                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in targetResponse.Headers)
                {
                    response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in targetResponse.Content.Headers)
                {
                    response.Headers[header.Key] = header.Value.ToArray();
                }

                // Rewrite Location headers (if any)
                if (response.Headers.ContainsKey("Location"))
                {
                    string? location = response.Headers["Location"];
                    location = location?.Replace(s_reportServerUrl, context.Request.Scheme + "://" + context.Request.Host + ReportPrefix);
                    response.Headers["Location"] = location;
                }

                // Strip headers ASP.NET Core doesn't allow manually
                response.Headers.Remove("transfer-encoding");


                response.Headers.Remove("Content-Encoding");
                response.Headers.Remove("Content-Length");

                response.ContentType = targetResponse.Content.Headers.ContentType?.ToString();

                byte[] responseBody = await targetResponse.Content.ReadAsByteArrayAsync();

                if (response.ContentType?.StartsWith("text/html", System.StringComparison.OrdinalIgnoreCase) == true)
                {
                    string html = System.Text.Encoding.UTF8.GetString(responseBody);
                    html = html.Replace("href=\"" + s_reportServerApplicationPath, "href=\"" + ReportPrefix);
                    html = html.Replace("src=\"" + s_reportServerApplicationPath, "src=\"" + ReportPrefix);
                    html = html.Replace(s_reportServerDomain + "/ReportServer", context.Request.Host + ReportPrefix);

                    await response.WriteAsync(html);
                }
                else
                {
                    await response.Body.WriteAsync(responseBody);
                }

                await response.CompleteAsync();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        } // End Task 


    } // End Class ReportProxyMiddleware 


} // End Namespace 
