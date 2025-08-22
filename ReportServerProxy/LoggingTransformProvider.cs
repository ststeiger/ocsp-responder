
namespace ReportServerProxy;

using System.Data.Common;
using System.Linq;
using Yarp.ReverseProxy.Transforms; // for AddRequestTransform, AddResponseTransform
using static System.Net.Mime.MediaTypeNames;

public class LoggingTransformProvider
    : Yarp.ReverseProxy.Transforms.Builder.ITransformProvider
{
    // Required in recent YARP versions
    public void ValidateRoute(Yarp.ReverseProxy.Transforms.Builder.TransformRouteValidationContext context)
    {
        // No custom validation needed for logging
    } // End Sub ValidateRoute 

    public void ValidateCluster(Yarp.ReverseProxy.Transforms.Builder.TransformClusterValidationContext context)
    {
        // No custom validation needed for logging
    } // End Sub ValidateCluster 

    public void Apply(Yarp.ReverseProxy.Transforms.Builder.TransformBuilderContext context)
    {
        context.AddRequestTransform(async transformContext =>
        {
            // is ignored by SSRS
            // transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", "/VIRT_DIR_X");

            Microsoft.AspNetCore.Http.HttpRequest request = transformContext.HttpContext.Request;
            Microsoft.AspNetCore.Http.HttpRequestRewindExtensions.EnableBuffering(request);

            using (System.IO.TextReader tr = new System.IO.StreamReader(
                       request.Body,
                       System.Text.Encoding.UTF8,
                       leaveOpen: true
                )
            )
            {
                string body = await tr.ReadToEndAsync();
                request.Body.Position = 0;

                if (body.IndexOf("/VIRT_DIR_X") != -1)
                {
                    System.Console.WriteLine(body);
                }


                System.Console.WriteLine($"[REQUEST] {request.Method} {request.Path}");
                System.Console.WriteLine(body);
                System.Console.WriteLine(System.Environment.NewLine);
                System.Console.WriteLine(System.Environment.NewLine);
            }

        }); // End AddRequestTransform 

        context.AddResponseTransform(async transformContext =>
        {
            // https://github.com/dotnet/yarp/issues/1109
            // transformContext.ProxyResponse.Headers:
            //     These are the original headers from the backend service response

            // transformContext.HttpContext.Response.Headers
            //     These are the headers that will actually be sent back to the client

            // When you modify transformContext.ProxyResponse.Headers,
            // you're changing the internal representation of the backend response,
            // but YARP has already copied these headers to the ASP.NET Core response headers
            // (HttpContext.Response.Headers), which are what actually get sent to the client.


            System.Net.Http.HttpResponseMessage? response = transformContext.ProxyResponse;


            // Only proceed if we have a valid response
            if (response != null)
            {
                // --- Handle Location Header Rewrite first, before any content processing ---
                System.Net.Http.Headers.HttpResponseHeaders headers = response.Headers;

                // We need to work with the output headers that will be sent to the client
                Microsoft.AspNetCore.Http.IHeaderDictionary outputHeaders = transformContext.HttpContext.Response.Headers;
                

                // Check if the response contains a Location header (indicates a redirect)
                if (headers.TryGetValues("Location", out var locationValues))
                {
                    // Redirects usually have only one Location header value
                    string originalLocation = locationValues.FirstOrDefault();

                    if (!string.IsNullOrEmpty(originalLocation))
                    {
                        // Try to parse the original Location header value as a URI
                        if (System.Uri.TryCreate(originalLocation, System.UriKind.Absolute, out System.Uri originalUri))
                        {
                            try
                            {
                                // Get the scheme (http) and host:port (localhost:12434) from the incoming request
                                string proxyScheme = transformContext.HttpContext.Request.Scheme;
                                Microsoft.AspNetCore.Http.HostString proxyHost = transformContext.HttpContext.Request.Host;

                                // Use UriBuilder to easily reconstruct the URI
                                System.UriBuilder newUriBuilder = new System.UriBuilder(originalUri);

                                // Set the scheme and host based on the incoming request (proxy)
                                newUriBuilder.Scheme = proxyScheme;
                                newUriBuilder.Host = proxyHost.Host;

                                if (newUriBuilder.Path != null && newUriBuilder.Path.StartsWith("/ReportServer", System.StringComparison.InvariantCultureIgnoreCase))
                                    newUriBuilder.Path = "/VIRT_DIR_X" + newUriBuilder.Path;


                                // Set the port based on the incoming request
                                if (proxyHost.Port.HasValue)
                                {
                                    newUriBuilder.Port = proxyHost.Port.Value;
                                }
                                else
                                {
                                    // If no port was specified in the incoming host header, set the default port
                                    newUriBuilder.Port = proxyScheme == "https" ? 443 : 80;
                                }

                                System.Uri newUri = newUriBuilder.Uri;

                                // Important: Modify the output headers instead of the proxy response headers
                                if (outputHeaders.ContainsKey("Location"))
                                {
                                    // Remove the original Location header
                                    outputHeaders.Remove("Location");
                                }

                                // Add the rewritten Location header to output headers
                                outputHeaders.Add("Location", newUri.OriginalString);

                                // Add debugging to verify this code is running
                                System.Console.WriteLine($"[REDIRECT] Rewrote Location header from '{originalLocation}' to '{newUri.OriginalString}'");
                            }
                            catch (System.Exception ex)
                            {
                                System.Console.WriteLine($"Error rewriting Location header: {ex.Message}");
                            }
                        }
                        else
                        {
                            if (originalLocation.StartsWith("/ReportServer", System.StringComparison.InvariantCultureIgnoreCase))
                            { 
                                // Important: Modify the output headers instead of the proxy response headers
                                if (outputHeaders.ContainsKey("Location"))
                                {
                                    // Remove the original Location header
                                    outputHeaders.Remove("Location");
                                }

                                string newLocation = "/VIRT_DIR_X" + originalLocation;
                                // Add the rewritten Location header to output headers
                                outputHeaders.Add("Location", newLocation);
                            }


                            System.Console.WriteLine($"Failed to parse Location header as URI: {originalLocation}");
                        }
                    }
                }

                // Now handle content logging
                if (response.Content != null)
                {
                    // Buffer the content into memory
                    byte[] originalContent = await response.Content.ReadAsByteArrayAsync();

                    // Log it
                    string contentString = System.Text.Encoding.UTF8.GetString(originalContent);


                    bool isGzip = false;

                    // Try to decompress if it's gzipped
                    foreach (string? thisEncoding in outputHeaders["Content-Encoding"])
                    {
                        if ("gzip".Equals(thisEncoding, System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            isGzip = true;
                    
                        }
                    }


                    if(isGzip)
                        contentString = GzipHelper.DecompressGzipBytesToString(originalContent, System.Text.Encoding.UTF8);


                    // if (outputHeaders.ContentType.ToString().Contains("text/html") == true)
                    if (contentString.IndexOf("/ReportServer") != -1)
                    {
                        System.Console.WriteLine(outputHeaders.ContentType);

                        string modified = contentString.Replace("src=\"/ReportServer", "src=\"/VIRT_DIR_X/ReportServer");
                        modified = modified.Replace("href=\"/ReportServer", "href=\"/VIRT_DIR_X/ReportServer");
                        modified = modified.Replace("url(\"/ReportServer", "url(\"/VIRT_DIR_X/ReportServer");
                        modified = modified.Replace("\":\"/ReportServer", "\":\"/VIRT_DIR_X/ReportServer");
                        modified = modified.Replace("\\\":\\\"/ReportServer", "\\\":\\\"/VIRT_DIR_X/ReportServer");


                        if (isGzip)
                            originalContent = await GzipHelper.Compress(modified, new System.Text.UTF8Encoding(false));
                        // else originalContent = System.Text.Encoding.UTF8.GetBytes(modified);
                    }

                    System.Console.WriteLine($"[RESPONSE] {transformContext.HttpContext.Request.Method} {transformContext.HttpContext.Request.Path}");
                    System.Console.WriteLine(contentString);

                    System.Console.WriteLine(System.Environment.NewLine);
                    System.Console.WriteLine(System.Environment.NewLine);

                    // Replace the original content with a new stream so YARP can still send it
                    response.Content = new System.Net.Http.ByteArrayContent(originalContent);
                } // End if (response.Content != null) 

            } // End if (response != null)

        }); // End AddResponseTransform 

    } // End Sub Apply 


} // End Class LoggingTransformProvider