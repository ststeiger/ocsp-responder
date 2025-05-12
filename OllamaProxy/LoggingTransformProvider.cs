
namespace OllamaProxy;


using Yarp.ReverseProxy.Transforms; // for AddRequestTransform, AddResponseTransform

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
                
                System.Console.WriteLine($"[REQUEST] {request.Method} {request.Path}");
                System.Console.WriteLine(body);
                System.Console.WriteLine(System.Environment.NewLine);
                System.Console.WriteLine(System.Environment.NewLine);
            }
            
        }); // End AddRequestTransform 

        context.AddResponseTransform(async transformContext =>
        {
            System.Net.Http.HttpResponseMessage? response = transformContext.ProxyResponse;

            if (response != null && response.Content != null)
            {
                // Buffer the content into memory
                byte[] originalContent = await response.Content.ReadAsByteArrayAsync();
                
                // Backup original headers
                System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>> originalHeaders = 
                    new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();

                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in response.Content.Headers)
                {
                    originalHeaders.Add(header.Key, header.Value);
                } // Next header 
                
                
                // Log it
                string contentString = System.Text.Encoding.UTF8.GetString(originalContent);
                System.Console.WriteLine($"[RESPONSE] {transformContext.HttpContext.Request.Method} {transformContext.HttpContext.Request.Path}");
                System.Console.WriteLine(contentString);
                
                System.Console.WriteLine(System.Environment.NewLine);
                System.Console.WriteLine(System.Environment.NewLine);
                
                // Replace the original content with a new stream so YARP can still send it
                response.Content = new System.Net.Http.ByteArrayContent(originalContent);
                
                // Restore headers
                foreach (System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.IEnumerable<string>> header in originalHeaders)
                {
                    response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                } // Next header 
                
            } // End if (response != null && response.Content != null) 
            
        }); // End AddResponseTransform 
        
    } // End Sub Apply 
    
    
} // End Class LoggingTransformProvider 