
namespace OcspResponder.Core.Internal
{


    /// <summary>
    /// Result of the attempt to retrieve the <see cref="OcspReq"/> from the <see cref="HttpRequestMessage"/>
    /// </summary>
    internal class OcspReqResult
    {
        /// <summary>
        /// Status of the request according to <see cref="OcspRespStatus"/>
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// The <see cref="OcspReq"/> extracted from the <see cref="HttpRequestMessage"/>
        /// </summary>
        public Org.BouncyCastle.Ocsp.OcspReq OcspRequest { get; set; }

        /// <summary>
        /// Error message if the <see cref="OcspReq"/> could not be retrieved
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Recognized issued certificate
        /// </summary>
        public Org.BouncyCastle.X509.X509Certificate IssuerCertificate { get; set; }
    }
}
