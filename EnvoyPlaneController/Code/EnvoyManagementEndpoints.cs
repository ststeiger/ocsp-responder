
namespace EnvoyPlaneController
{
    using Dapper;
    using EnvoyPlaneController.Code;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Npgsql;
    using System.Text.Json;

    public static class EnvoyManagementEndpoints 
    {

        public static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapExampleEndpoint2(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints,
            [System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
            string pattern
        )
        {
#if false
            string connectionString = "";

            endpoints.MapGet("/backend", () =>
            {
                return Microsoft.AspNetCore.Http.Results.Json(clustersCache.Values);
            });



            // HTTP endpoint to add/update backend
            endpoints.MapPost("/backend", async (HttpContext ctx) =>
            {
                var config = await JsonSerializer.DeserializeAsync<ClusterConfig>(ctx.Request.Body);
                if (config == null) return Results.BadRequest();

                // Save to DB
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.ExecuteAsync(@"
        INSERT INTO backend_routes(host, address, port)
        VALUES(@Host, @Address, @Port)
        ON CONFLICT(host) DO UPDATE SET address = @Address, port = @Port
    ", config);

                // Update in-memory cache
                clustersCache[config.Host] = config;

                // Push to Envoy
                await PushClustersToEnvoyAsync(clustersCache.Values);

                return Results.Ok(config);
            });

            // HTTP endpoint to remove backend
            endpoints.MapDelete("/backend/{host}", async (string host) =>
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.ExecuteAsync("DELETE FROM backend_routes WHERE host = @Host", new { Host = host });

                clustersCache.TryRemove(host, out _);

                await PushClustersToEnvoyAsync(clustersCache.Values);

                return Results.Ok(host);
            });

#endif
            return endpoints;
        } // End Extension Method MapReportProxy 


        public static async System.Threading.Tasks.Task ProxyRequest(
              Microsoft.AspNetCore.Http.HttpContext context
        )
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }

}

