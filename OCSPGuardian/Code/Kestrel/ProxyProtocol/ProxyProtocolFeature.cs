// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace OCSPGuardian.ProxyProtocol
{
    public class ProxyProtocolFeature
        : IProxyProtocolFeature
    {
        public System.Net.IPAddress? SourceIp { get; internal set; }
        public System.Net.IPAddress? DestinationIp { get; internal set; }
        public int? SourcePort { get; internal set; }
        public int? DestinationPort { get; internal set; }
        public long? LinkId { get; internal set; }


        /// <summary>
        /// TLV (Type-Length-Value) section of the PPv2 header of the Proxy-V2-protocol, where additional optional values reside. 
        /// </summary>
        public byte[]? TLV { get; internal set; }
    }
}