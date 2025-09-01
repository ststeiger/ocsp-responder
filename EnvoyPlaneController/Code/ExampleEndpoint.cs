
namespace EnvoyPlaneController
{
    using Microsoft.AspNetCore.Builder;


    public static class ExampleEndpoint
    {

        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapExampleEndpoint(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
            [System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
                    string pattern
        )
        {
            return endpoints.MapMethods(
                pattern,
                new string[] { "GET", "POST", "DELETE", "PUT", "PATCH", "OPTIONS" },
                ProxyRequest
            );
        } // End Extension Method MapReportProxy 


        public static async System.Threading.Tasks.Task ProxyRequest(
              Microsoft.AspNetCore.Http.HttpContext context
        )
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }

}

