
namespace TestHttpsProxy
{


    // Define route mapping (SNI hostname -> backend endpoint)
    public class RouteTable
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Net.IPEndPoint> _routes = new();

        public bool TryGet(string host, out System.Net.IPEndPoint endpoint)
            => _routes.TryGetValue(host, out endpoint);

        public void AddOrUpdate(string host, System.Net.IPEndPoint endpoint)
            => _routes[host] = endpoint;

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Net.IPEndPoint>> GetAll()
            => _routes.ToArray();
    }


}
