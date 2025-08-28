
namespace ReportServerProxyCore
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Linq;


    public static class ReportProxyEndpoint
    {
        private const string ReportPrefix = "/ReportServer";

        private static readonly string s_reportServerDomain = "reportsrv2.cor-asp.ch";
        private static readonly string s_reportServerApplicationPath = "/ReportServer";
        private static readonly string s_reportServerUrl = "https://" + s_reportServerDomain + s_reportServerApplicationPath;


        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapReportProxy(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
            [System.Diagnostics.CodeAnalysis.StringSyntax("Route")] 
            string pattern
        )
        {
            return endpoints.MapMethods(
                pattern,
                new string[] { "GET", "POST", "DELETE", "PUT", "PATCH", "OPTIONS" },
                ReportProxyEndpoint.ProxyRequest
            );
        } // End Extension Method MapReportProxy 


        private static string GetPrefix(Microsoft.AspNetCore.Http.HttpContext context)
        {
#if ENABLE_WITH_CONTROLLERS
            Endpoint? endpoint = context.GetEndpoint();
            // The routePattern = endpoint?.Metadata.GetMetadata<RoutePattern>() part
            // is only relevant if you ever mix in MVC controllers.
            Microsoft.AspNetCore.Routing.Patterns.RoutePattern? routePattern = 
                endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.Patterns.RoutePattern>();
            
            string? rawPattern = routePattern?.RawText;

            // Here we fix for Minimal routiner 
            if (rawPattern == null)
            {
                Microsoft.AspNetCore.Routing.RouteEndpoint? minimalEndpoint = endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint;
                rawPattern = minimalEndpoint?.RoutePattern.RawText;
            } // End if (rawPattern == null) 
#else 
            // efficiency: we don't mix with controllers, so this is more efficient. 
            Microsoft.AspNetCore.Routing.RouteEndpoint? routeEndpoint = context.GetEndpoint() as Microsoft.AspNetCore.Routing.RouteEndpoint;
            string? rawPattern = routeEndpoint?.RoutePattern.RawText;
#endif 

            if (string.IsNullOrEmpty(rawPattern))
                return string.Empty;

            int ind = rawPattern.IndexOf("{*");
            
            

            if (ind == -1)
                return string.Empty;

            // Prefix before the catch-all
            // string prefix = rawPattern.Substring(0, ind).TrimEnd('/');
            // return prefix;

            // Micro-Optimization: only ONE allocation for substring and TrimEnd instead of 2 
            System.ReadOnlySpan<char> span = rawPattern.AsSpan();
            System.ReadOnlySpan<char> prefixSpan = span.Slice(0, ind).TrimEnd('/');

            // Only allocate here for the final string
            return prefixSpan.ToString();
        } // End Function GetPrefix 


        public static async System.Threading.Tasks.Task ProxyRequest(
            Microsoft.AspNetCore.Http.HttpContext context, 
            System.Net.Http.IHttpClientFactory httpClientFactory
            // ,string? catchAll // this includes only the path, not the queryString
        )
        {

            try
            {
                string prefix = GetPrefix(context);
                System.Diagnostics.Debug.WriteLine(prefix);

                Microsoft.AspNetCore.Http.HttpRequest request = context.Request;
                Microsoft.AspNetCore.Http.HttpResponse response = context.Response;
                System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient("ReportProxy");


                // string? catchAll = request.Path.Value?.Substring(ReportPrefix.Length) + request.QueryString;
                string? catchAll = request.Path.Value?.Substring(prefix.Length) + request.QueryString;
                
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
                        targetResponse = await httpClient.SendAsync(targetRequest, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
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
