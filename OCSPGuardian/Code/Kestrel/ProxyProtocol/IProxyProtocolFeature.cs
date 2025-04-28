
namespace OCSPGuardian.ProxyProtocol
{

    public interface IProxyProtocolFeature
    {
        /// <summary>
        /// This is the IP of the client machine (browser) as received by the proxy server 
        /// </summary>
        System.Net.IPAddress? SourceIp { get; }

        /// <summary>
        /// This is the IP of the reverse-proxy server that received the client's connection (proxy-server listen IP).
        /// </summary>
        System.Net.IPAddress? DestinationIp { get; }

        /// <summary>
        /// This is the port on the client machine (browser) as received by the proxy server 
        /// </summary>
        int? SourcePort { get; }

        /// <summary>
        /// This is the port on which the reverse-proxy server received the client's connection (proxy-server listen port).
        /// </summary>
        int? DestinationPort { get; }

        /// <summary>
        ///  linkId is an optional 4-byte (32-bit) value, specifically, it's part of the TLV (Type-Length-Value) section of the PPv2 header of the Proxy-V2-protocol, 
        ///  and is a unique identifier assigned to a private endpoint connection within Azure Private Link Service, which is used to identify the consumer of a service. 
        /// </summary>
        long? LinkId { get; }

        /// <summary>
        /// TLV (Type-Length-Value) section of the PPv2 header of the Proxy-V2-protocol, where additional optional values reside. 
        /// </summary>
        byte[]? TLV { get; }

    }


}
