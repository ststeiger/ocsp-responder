namespace EnvoyPlaneController.Code
{

    using Grpc.Core; // for ReadAllAsync 


    // The control plane is the component that manages the configuration of the data plane.
    // In Envoy:
    // Control Plane = your app (e.g., EnvoyManager) that tells Envoy what to do — what clusters to route to, what endpoints exist, etc.
    // Data Plane = Envoy proxies, which process network traffic according to that configuration.

    // Note: Role reversal: 
    // ControlPlaneServer is the server where Envoy gets the data from 
    // Envoy is the client, not the server 
    // the connection is persistent (push updates) 
    // Envoy opens the stream and maintains it; the control plane responds and pushes updates. 

    // Terminology:
    //  - Control Plane(s): One or more services that provide dynamic configuration to Envoy. 
    //  - Data Plane(s): The actual Envoy proxy instances (aka the Envoy instance). 
    //                   These are the processes handling traffic — acting as reverse proxies, load balancers, etc.
    //  - Clusters: in Envoy, clusters are logical groups of upstream hosts (like a pool of backends).
    //              A cluster can have one or more endpoints (IP+port).
    //  - The data plane doesn’t form a “cluster” of Envoy nodes (that's a common confusion).

    // CDS: (clusters)
    // RDS: (routes)


    // Node is the Name/ID of the Envoy instance (the data plane).
    // It's sent by Envoy in DiscoveryRequest messages to identify itself.
    // The control plane uses this to know *who* is asking for config.
    // like one NODE is assigned to a domain, such as www.example.com
    // (including possible example.com and *.example.com)
    // and then i have a list of backend servers that are "the cluster"
    // The node-id is important, because the control-plane needs it
    // to tell which backend servers to return for the cluster

    public class ControlPlaneServer
    {


        public static void Test()
        {
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    Envoy.Service.Discovery.V3.AggregatedDiscoveryService.BindService(new SimpleControlPlane())
                },
                Ports = { new Grpc.Core.ServerPort("0.0.0.0", 18000, Grpc.Core.ServerCredentials.Insecure) }
            };

            server.Start();

            System.Console.WriteLine("Control plane server started on port 18000.");
            System.Console.ReadLine();
        } // End Sub Test 


    } // End Class ControlPlaneServer 



    public class SimpleControlPlane
        : Envoy.Service.Discovery.V3.AggregatedDiscoveryService.AggregatedDiscoveryServiceBase
    {
        // Track connected streams by node ID
        private readonly System.Collections.Concurrent.ConcurrentDictionary<
            string, Grpc.Core.IServerStreamWriter<Envoy.Service.Discovery.V3.DiscoveryResponse>> _clients
            = new();

        private int _version = 1;

        public override async System.Threading.Tasks.Task StreamAggregatedResources(
            Grpc.Core.IAsyncStreamReader<Envoy.Service.Discovery.V3.DiscoveryRequest> requestStream,
            Grpc.Core.IServerStreamWriter<Envoy.Service.Discovery.V3.DiscoveryResponse> responseStream,
            Grpc.Core.ServerCallContext context)
        {
            string? nodeId = null;

            await foreach (Envoy.Service.Discovery.V3.DiscoveryRequest? request in requestStream.ReadAllAsync())
            {
                if (string.IsNullOrEmpty(nodeId) && request.Node != null)
                {
                    nodeId = request.Node.Id;
                    _clients[nodeId] = responseStream;

                    System.Console.WriteLine($"Envoy connected: {nodeId}");
                }

                System.Console.WriteLine($"Received DiscoveryRequest (type: {request.TypeUrl}) from {nodeId}");

                // Very basic response logic — only handle CDS (clusters) here
                if (request.TypeUrl == "type.googleapis.com/envoy.config.cluster.v3.Cluster")
                {
                    Envoy.Service.Discovery.V3.DiscoveryResponse response = CreateClusterDiscoveryResponse();
                    await responseStream.WriteAsync(response);
                    System.Console.WriteLine($"Sent CDS response (version {response.VersionInfo}) to {nodeId}");
                }
            }

            if (nodeId != null)
            {
                _clients.TryRemove(nodeId, out _);
                System.Console.WriteLine($"Envoy disconnected: {nodeId}");
            }
        }

        private Envoy.Service.Discovery.V3.DiscoveryResponse CreateClusterDiscoveryResponse()
        {
            // Build a simple cluster
            Envoy.Config.Cluster.V3.Cluster cluster = new Envoy.Config.Cluster.V3.Cluster
            {
                Name = "example-cluster",
                ConnectTimeout = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(System.TimeSpan.FromSeconds(1)),
                Type = Envoy.Config.Cluster.V3.Cluster.Types.DiscoveryType.StrictDns,
                LbPolicy = Envoy.Config.Cluster.V3.Cluster.Types.LbPolicy.RoundRobin,
                LoadAssignment = new Envoy.Config.Endpoint.V3.ClusterLoadAssignment
                {
                    ClusterName = "example-cluster",
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
                                            Address = "127.0.0.1",
                                            PortValue = 8080
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                }
            };

            return new Envoy.Service.Discovery.V3.DiscoveryResponse()
            {
                VersionInfo = _version++.ToString(),
                TypeUrl = "type.googleapis.com/envoy.config.cluster.v3.Cluster",
                Resources = { Google.Protobuf.WellKnownTypes.Any.Pack(cluster) },
                Nonce = System.Guid.NewGuid().ToString()
            };
        }

        // You could extend this to support pushing updates to all connected Envoys
        public async System.Threading.Tasks.Task BroadcastClusterUpdateAsync()
        {
            Envoy.Service.Discovery.V3.DiscoveryResponse response = CreateClusterDiscoveryResponse();

            foreach (System.Collections.Generic.KeyValuePair<
                string, IServerStreamWriter<Envoy.Service.Discovery.V3.DiscoveryResponse>
                > kvp in _clients)
            {
                try
                {
                    await kvp.Value.WriteAsync(response);
                    System.Console.WriteLine($"Pushed cluster update to {kvp.Key}");
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Failed to push update to {kvp.Key}: {ex.Message}");
                }
            } // Next kvp 

        } // End Task BroadcastClusterUpdateAsync 


    } // End Class SimpleControlPlane 


} // End Namespace 
