
namespace ReportServerProxyCore
{


    public static class ReportProxyEndpoint
    {
        
        private static readonly string s_reportServerDomain = "reportsrv2.cor-asp.ch";
        private static readonly string s_reportServerApplicationPath = "/ReportServer";
        private static readonly string s_reportServerUrl = "https://" + s_reportServerDomain + s_reportServerApplicationPath;


        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapReportProxy(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
            [System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
            string pattern
        )
        {
            return Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapMethods(endpoints, pattern,
                new string[] { "GET", "POST", "DELETE", "PUT", "PATCH", "OPTIONS" },
                ReportProxyEndpoint.ProxyRequest
            );
        } // End Extension Method MapReportProxy 


        private static string GetPrefix(Microsoft.AspNetCore.Http.HttpContext context)
        {
#if ENABLE_WITH_CONTROLLERS
            Microsoft.AspNetCore.Http.Endpoint? endpoint = Microsoft.AspNetCore.Http.EndpointHttpContextExtensions.GetEndpoint(context);
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
            Microsoft.AspNetCore.Routing.RouteEndpoint? routeEndpoint = Microsoft.AspNetCore.Http.EndpointHttpContextExtensions.GetEndpoint(context) as Microsoft.AspNetCore.Routing.RouteEndpoint;
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
            System.ReadOnlySpan<char> span = System.MemoryExtensions.AsSpan( rawPattern);
            System.ReadOnlySpan<char> prefixSpan = System.MemoryExtensions.TrimEnd(span.Slice(0, ind), '/');

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
                string ReportPrefix = GetPrefix(context);
                System.Diagnostics.Debug.WriteLine(ReportPrefix);

                Microsoft.AspNetCore.Http.HttpRequest request = context.Request;
                Microsoft.AspNetCore.Http.HttpResponse response = context.Response;
                System.Net.Http.HttpClient httpClient = httpClientFactory.CreateClient("ReportProxy");


                // string? catchAll = request.Path.Value?.Substring(ReportPrefix.Length) + request.QueryString;
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
                if (!string.IsNullOrEmpty(ua))
                    targetRequest.Headers.UserAgent.ParseAdd(ua);


                // Copy the Accept-Language header from the incoming request to the outgoing request
                string language = request.Headers["Accept-Language"].ToString();
                if (!string.IsNullOrEmpty(language))
                    targetRequest.Headers.AcceptLanguage.ParseAdd(language);


                // Copy request headers
                foreach (System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers)
                {
                    if (!System.Net.WebHeaderCollection.IsRestricted(header.Key) &&
                        !header.Key.StartsWith(":") &&
                        !string.Equals(header.Key, "Host", System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                } // Next header 

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
                        } // End Using ms 

                    } // End if (request.ContentLength > 0 && request.Body.CanRead) 

                    try
                    {
                        targetResponse = await httpClient.SendAsync(targetRequest, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        response.StatusCode = 502;
                        await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(response, "Proxy Error: " + ex.Message);

                        return;
                    }
                }
                response.StatusCode = (int)targetResponse.StatusCode;


                // Copy response headers
                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in targetResponse.Headers)
                {
                    // response.Headers[header.Key] = header.Value.ToArray();
                    //Microsoft.Extensions.Primitives.StringValues existingStringValues = response.Headers[header.Key];

                    // This is the alternative to header.Value.ToArray()
                    System.Collections.Generic.List<string> ls = new System.Collections.Generic.List<string>();
                    foreach (string value in header.Value)
                        ls.Add(value);

                    // Then you would assign it
                    // response.Headers[header.Key] = existingStringValues + ls;
                    response.Headers[header.Key] = new Microsoft.Extensions.Primitives.StringValues(ls.ToArray());
                } // Next header 


                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in targetResponse.Content.Headers)
                {
                    // response.Headers[header.Key] = header.Value.ToArray();

                    //Microsoft.Extensions.Primitives.StringValues existingStringValues = response.Headers[header.Key];

                    // This is the alternative to header.Value.ToArray()
                    System.Collections.Generic.List<string> ls = new System.Collections.Generic.List<string>();
                    foreach (string value in header.Value)
                        ls.Add(value);

                    // Then you would assign it
                    // response.Headers[header.Key] = existingStringValues + ls;
                    response.Headers[header.Key] = new Microsoft.Extensions.Primitives.StringValues(ls.ToArray());
                } // Next header 

                // Rewrite Location headers (if any)
                if (response.Headers.ContainsKey("Location"))
                {
                    string? location = response.Headers["Location"];
                    location = location?.Replace(s_reportServerUrl, context.Request.Scheme + "://" + context.Request.Host + ReportPrefix);
                    response.Headers["Location"] = location;
                } // End if (response.Headers.ContainsKey("Location")) 

                // Strip headers ASP.NET Core doesn't allow manually
                response.Headers.Remove("transfer-encoding");
                response.Headers.Remove("Content-Encoding");
                response.Headers.Remove("Content-Length");

                response.ContentType = targetResponse.Content.Headers.ContentType?.ToString();

                byte[] responseBody = await targetResponse.Content.ReadAsByteArrayAsync();

                if (response.ContentType?.StartsWith("text/html", System.StringComparison.OrdinalIgnoreCase) == true)
                {
                    string responseText = System.Text.Encoding.UTF8.GetString(responseBody);
                    responseText = responseText.Replace("href=\"" + s_reportServerApplicationPath, "href=\"" + ReportPrefix);
                    responseText = responseText.Replace("src=\"" + s_reportServerApplicationPath, "src=\"" + ReportPrefix);
                    responseText = responseText.Replace(s_reportServerDomain + "/ReportServer", context.Request.Host + ReportPrefix);

                    responseText = responseText.Replace("url(\"/ReportServer", "url(\"" + ReportPrefix);
                    responseText = responseText.Replace("Url\":\"/ReportServer", "Url\":\"" + ReportPrefix);
                    responseText = responseText.Replace("\\\":\\\"/ReportServer", "\\\":\\\"" + ReportPrefix);
                    responseText = responseText.Replace("\":\"/ReportServer", "\":\"" + ReportPrefix);

                    await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(response, responseText);
                } // End if text/html
                else if (response.ContentType?.StartsWith("text/plain", System.StringComparison.OrdinalIgnoreCase) == true)
                {
                    string responseText = System.Text.Encoding.UTF8.GetString(responseBody);
                    System.Collections.Generic.List<AjaxDelta> parsedDeltas = AjaxDeltaParser.Parse(responseText);

                    foreach (AjaxDelta thisDelta in parsedDeltas)
                    {
                        if (thisDelta.Content == null)
                            continue;

                        if (thisDelta.Content.ToLower().IndexOf("reportserver") != -1)
                        {
                            thisDelta.Content = thisDelta.Content.Replace("/ReportServer", ReportPrefix);
                        } // End if 

                    } // Next thisDelta 

                    responseText = AjaxDeltaParser.Recombine(parsedDeltas);
                    await Microsoft.AspNetCore.Http.HttpResponseWritingExtensions.WriteAsync(response, responseText);
                } // End if text/plain 
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
        } // End Task ProxyRequest 


    } // End Class ReportProxyMiddleware 


} // End Namespace 
