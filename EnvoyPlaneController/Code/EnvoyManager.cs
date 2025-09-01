
// https://learn.microsoft.com/en-us/aspnet/core/grpc/client?view=aspnetcore-9.0
namespace EnvoyPlaneController.Code
{


    using Dapper;
    using Google.Protobuf.WellKnownTypes;
    // add package Grpc.Net.Client 
    using Grpc.Core; // for ReadAllAsync
    // using Envoy.Service.ClusterDiscovery.V3; // old 


    public class EnvoyManager 
    {


        public static async System.Threading.Tasks.Task Test()
        {
            // PostgreSQL connection string
            string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=secret;Database=envoydb";

            // Envoy.Service.Discovery.V3.


            // gRPC channel to Envoy xDS server
            Grpc.Net.Client.GrpcChannel channel = Grpc.Net.Client.
                GrpcChannel.ForAddress("http://127.0.0.1:18000"); // Envoy CDS gRPC port

            // var cdsClient = new ClusterDiscoveryService.ClusterDiscoveryServiceClient(channel);
            Envoy.Service.Discovery.V3.AggregatedDiscoveryService.AggregatedDiscoveryServiceClient cdsClient =
                new Envoy.Service.Discovery.V3.AggregatedDiscoveryService.AggregatedDiscoveryServiceClient(channel);

            // In-memory cache of current clusters to simplify updates
            System.Collections.Concurrent.ConcurrentDictionary<string, ClusterConfig> clustersCache = new();

            // Load DB entries on startup
            await using (System.Data.Common.DbConnection cnn = new Npgsql.NpgsqlConnection(connectionString))
            {
                System.Collections.Generic.IEnumerable<ClusterConfig> rows = await cnn.QueryAsync<ClusterConfig>(
                    "SELECT host, address, port FROM backend_routes"
                );

                foreach (ClusterConfig row in rows)
                {
                    clustersCache[row.Host] = row;
                } // Next row 

                await PushClustersToEnvoyAsync(cdsClient, clustersCache.Values);
            } // End Using cnn 

        } // End Task Test 


        public static async System.Threading.Tasks.Task PushClustersToEnvoyAsync(
             Envoy.Service.Discovery.V3.AggregatedDiscoveryService.AggregatedDiscoveryServiceClient cdsClient,
             System.Collections.Generic.IEnumerable<ClusterConfig> clusters
        )
        {
            // Create a streaming call
            using Grpc.Core.AsyncDuplexStreamingCall<
                Envoy.Service.Discovery.V3.DiscoveryRequest, Envoy.Service.Discovery.V3.DiscoveryResponse
                > call = cdsClient.StreamAggregatedResources();

            int clusterCount = 0;

#if USE_LINQ  
            // Pack each cluster into Any
            System.Collections.Generic.IEnumerable<Google.Protobuf.WellKnownTypes.Any> resources = clusters.Select(c => Google.Protobuf.WellKnownTypes.Any.Pack(
                new Envoy.Config.Cluster.V3.Cluster
                {
                    Name = c.Host,
                    ConnectTimeout = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(System.TimeSpan.FromSeconds(1)),
                    Type = Envoy.Config.Cluster.V3.Cluster.Types.DiscoveryType.StrictDns,
                    LbPolicy = Envoy.Config.Cluster.V3.Cluster.Types.LbPolicy.RoundRobin,
                    LoadAssignment = new Envoy.Config.Endpoint.V3.ClusterLoadAssignment
                    {
                        ClusterName = c.Host,
                        Endpoints =
                        {
                            new Envoy.Config.Endpoint.V3.LocalityLbEndpoints
                            {
                                LbEndpoints =
                                {
                                    new Envoy.Config.Endpoint.V3.LbEndpoint
                                    {
                                        Endpoint = new Envoy.Config.Endpoint.V3.Endpoint
                                        {
                                            Address = new Envoy.Config.Core.V3.Address
                                            {
                                                SocketAddress = new Envoy.Config.Core.V3.SocketAddress
                                                {
                                                    Address = c.Address,
                                                    PortValue = (uint)c.Port
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }));

            clusterCount = clusters.AsList().Count; // Thanks Dapper 
#else

            System.Collections.Generic.List<Google.Protobuf.WellKnownTypes.Any> resources = new System.Collections.Generic.List<Google.Protobuf.WellKnownTypes.Any>();

            foreach (ClusterConfig cluster in clusters)
            {
                clusterCount++;

                Envoy.Config.Cluster.V3.Cluster envoyCluster = new Envoy.Config.Cluster.V3.Cluster()
                {
                    Name = cluster.Host,
                    ConnectTimeout = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(System.TimeSpan.FromSeconds(1)),
                    Type = Envoy.Config.Cluster.V3.Cluster.Types.DiscoveryType.StrictDns,
                    LbPolicy = Envoy.Config.Cluster.V3.Cluster.Types.LbPolicy.RoundRobin,
                    LoadAssignment = new Envoy.Config.Endpoint.V3.ClusterLoadAssignment
                    {
                        ClusterName = cluster.Host,
                        Endpoints =
                        {
                            new Envoy.Config.Endpoint.V3.LocalityLbEndpoints
                            {
                                LbEndpoints =
                                {
                                    new Envoy.Config.Endpoint.V3.LbEndpoint
                                    {
                                        Endpoint = new Envoy.Config.Endpoint.V3.Endpoint
                                        {
                                            Address = new Envoy.Config.Core.V3.Address
                                            {
                                                SocketAddress = new Envoy.Config.Core.V3.SocketAddress()
                                                {
                                                    Address =  cluster.Address,
                                                    PortValue = (uint) cluster.Port
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                Google.Protobuf.WellKnownTypes.Any resource = Google.Protobuf.WellKnownTypes.Any.Pack(envoyCluster);
                resources.Add(resource);
            } // Next clusterConfig 
#endif 


            // Build DiscoveryResponse (to push clusters)
            Envoy.Service.Discovery.V3.DiscoveryResponse response = new Envoy.Service.Discovery.V3.DiscoveryResponse
            {
                VersionInfo = System.DateTime.UtcNow.Ticks.ToString(),
                TypeUrl = "type.googleapis.com/envoy.config.cluster.v3.Cluster",
            };
            response.Resources.AddRange(resources);

            // Send the response to Envoy
            await call.RequestStream.WriteAsync(new Envoy.Service.Discovery.V3.DiscoveryRequest
            {
                Node = new Envoy.Config.Core.V3.Node { Id = "control-plane-1", Cluster = "control-plane" },
                TypeUrl = response.TypeUrl,
                VersionInfo = response.VersionInfo,
                // Some Envoy packages allow setting resources here; otherwise, use `StreamAggregatedResources` in a management plane loop
            });

            System.Console.WriteLine($"Pushed {clusterCount} clusters to Envoy");
        } // End Task PushClustersToEnvoyAsync 


        public static async System.Threading.Tasks.Task GetClustersFromEnvoyAsync(
            Envoy.Service.Discovery.V3.AggregatedDiscoveryService.AggregatedDiscoveryServiceClient cdsClient,
            System.Collections.Generic.IEnumerable<ClusterConfig> clusters
        )
        {
            using Grpc.Core.AsyncDuplexStreamingCall<
                Envoy.Service.Discovery.V3.DiscoveryRequest, Envoy.Service.Discovery.V3.DiscoveryResponse
                > call = cdsClient.StreamAggregatedResources();

            Envoy.Service.Discovery.V3.DiscoveryRequest cdsRequest = new Envoy.Service.Discovery.V3.DiscoveryRequest()
            {

                // TypeUrl: Required
                // Tells the control plane what type of resource Envoy wants.
                // Example:
                // Clusters: "type.googleapis.com/envoy.config.cluster.v3.Cluster"
                // Endpoints: "type.googleapis.com/envoy.config.endpoint.v3.ClusterLoadAssignment"
                TypeUrl = "type.googleapis.com/envoy.config.cluster.v3.Cluster",

                // ResourceNames: Optional.
                // Tells the control plane: "I only want info about these specific resources (e.g., these cluster names)."
                // If left empty, it means: "Give me all of them."


                // VersioInfo: Envoy sends this to say: "I'm currently using version X of this config."
                // If this doesn't match the control plane’s latest version, the control plane will send a new DiscoveryResponse.
                // You can use a timestamp or a counter (like you did).
                VersionInfo = System.DateTime.UtcNow.Ticks.ToString()

                // Node: Describes the Envoy node making the request.
                // ,Node = new Envoy.Config.Core.V3.Node()
                // {
                //     Id = "control-plane-1",
                //     Cluster = "control-plane"
                // }
            };


            // Write request
            await call.RequestStream.WriteAsync(cdsRequest);

            // Read response
            await foreach (Envoy.Service.Discovery.V3.DiscoveryResponse? response in call.ResponseStream.ReadAllAsync())
            {
                foreach (Google.Protobuf.WellKnownTypes.Any? resource in response.Resources)
                {
                    // Each resource is an Any, unpack it
                    Envoy.Config.Cluster.V3.Cluster cluster;

                    if (resource.TryUnpack(out cluster))
                    {
                        System.Console.WriteLine($"Envoy cluster: {cluster.Name}");
                    }

                } // Next resource 

            } // Next response 

        } // End Task GetClustersFromEnvoyAsync 


    } // End Class EnvoyManager 


} // End Namespace 
