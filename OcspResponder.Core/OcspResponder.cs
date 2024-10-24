
namespace OcspResponder.Core
{

    using global::OcspResponder.Core.Internal;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Ocsp;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Ocsp;
    using Org.BouncyCastle.X509;


    /// <summary>
    /// Implementation of an OCSP responder as defined in RFC 6960
    /// <remarks>https://tools.ietf.org/html/rfc6960</remarks>
    /// </summary>
    public class OcspResponder 
        : IOcspResponder
    {
        public async System.Threading.Tasks.Task<OcspHttpResponse> Respond(OcspHttpRequest httpRequest)
        {
            try
            {
                OcspReqResult ocspReqResult = await GetOcspRequest(httpRequest);
                if (ocspReqResult.Status != OcspRespStatus.Successful)
                {
                    Log.Warn(ocspReqResult.Error);
                    return CreateResponse(OcspResponseGenerator.Generate(ocspReqResult.Status, null).GetEncoded());
                }

                OcspResp ocspResponse = await GetOcspDefinitiveResponse(ocspReqResult.OcspRequest, ocspReqResult.IssuerCertificate);
                return CreateResponse(ocspResponse.GetEncoded());
            }
            catch (System.Exception e)
            {
                Log.Error(e.Message);
                return CreateResponse(OcspResponseGenerator.Generate(OcspRespStatus.InternalError, null).GetEncoded());
            }
        }

        /// <summary>
        /// Gets the <see cref="OcspResp"/> for the <see cref="OcspReq"/>
        /// </summary>
        /// <param name="ocspRequest"></param>
        /// <param name="issuerCertificate"></param>
        /// <returns></returns>
        private async System.Threading.Tasks.Task<OcspResp> GetOcspDefinitiveResponse(OcspReq ocspRequest, X509Certificate issuerCertificate)
        {
            var basicResponseGenerator = new BasicOcspRespGenerator(
                new RespID(
                    await OcspResponderRepository.GetResponderPublicKey(issuerCertificate)));

            var extensionsGenerator = new X509ExtensionsGenerator();

            var nextUpdate = await OcspResponderRepository.GetNextUpdate();
            foreach (var request in ocspRequest.GetRequestList())
            {
                var certificateId = request.GetCertID();
                var serialNumber = certificateId.SerialNumber;

                CertificateStatus certificateStatus;
                CaCompromisedStatus caCompromisedStatus = await OcspResponderRepository.IsCaCompromised(issuerCertificate);
                if (caCompromisedStatus.IsCompromised)
                {
                    // See section 2.7 of RFC 6960
                    certificateStatus = new RevokedStatus(caCompromisedStatus.CompromisedDate.Value.UtcDateTime, (int)RevocationReason.CACompromise);
                }
                else
                {
                    // Se section 2.2 of RFC 6960
                    if (await OcspResponderRepository.SerialExists(serialNumber, issuerCertificate))
                    {
                        var status = await OcspResponderRepository.SerialIsRevoked(serialNumber, issuerCertificate);
                        certificateStatus = status.IsRevoked
                            ? new RevokedStatus(status.RevokedInfo.Date.UtcDateTime, (int)status.RevokedInfo.Reason)
                            :  CertificateStatus.Good;
                    }
                    else
                    { 
                        certificateStatus = new RevokedStatus(new System.DateTime(1970, 1, 1), CrlReason.CertificateHold);
                        extensionsGenerator.AddExtension(OcspObjectIdentifierExtensions.PkixOcspExtendedRevoke, false, DerNull.Instance.GetDerEncoded());
                    }
                }

                basicResponseGenerator.AddResponse(certificateId, certificateStatus, System.DateTimeOffset.UtcNow.DateTime, nextUpdate.UtcDateTime, null);
            }

            SetNonceExtension(ocspRequest, extensionsGenerator);

            basicResponseGenerator.SetResponseExtensions(extensionsGenerator.Generate());

            // Algorithm that all clients shall accept as defined in section 4.3 of RFC 6960
            const string signatureAlgorithm = "sha256WithRSAEncryption";
            var basicOcspResponse = basicResponseGenerator.Generate(
                signatureAlgorithm,
                await OcspResponderRepository.GetResponderPrivateKey(issuerCertificate),
                await OcspResponderRepository.GetChain(issuerCertificate),
                nextUpdate.UtcDateTime);

            var ocspResponse = OcspResponseGenerator.Generate(OcspRespStatus.Successful, basicOcspResponse);
            return ocspResponse;
        }

        /// <summary>
        /// Add nonce extension if it exists in the request
        /// </summary>
        /// <param name="ocspRequest">ocsp request</param>
        /// <param name="extensionsGenerator">extensions generator</param>
        private void SetNonceExtension(OcspReq ocspRequest, X509ExtensionsGenerator extensionsGenerator)
        {
            var nonce = ocspRequest.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNonce);
            if (nonce != null)
            {
                extensionsGenerator.AddExtension(OcspObjectIdentifiers.PkixOcspNonce, false, nonce.GetOctets());
            }
        }

        /// <summary>
        /// Retrieves the <see cref="OcspReq"/> from the request
        /// </summary>
        /// <param name="httpRequest"><see cref="OcspHttpRequest"/></param>
        /// <returns><see cref="OcspReqResult"/> containing the <see cref="OcspReq"/></returns>
        private async System.Threading.Tasks.Task<OcspReqResult> GetOcspRequest(OcspHttpRequest httpRequest)
        {
            // Validates the header of the request
            if (httpRequest.MediaType != "application/ocsp-request")
            {
                return new OcspReqResult
                {
                    Status = OcspRespStatus.MalformedRequest,
                    Error = "OCSP requests requires 'application/ocsp-request' media's type on header"
                };
            }

            // Try to create the ocsp from the http request
            OcspReq ocspRequest;
            try
            {
                ocspRequest = CreateOcspReqFromHttpRequest(httpRequest);
            }
            catch(System.Exception e)
            {
                return new OcspReqResult
                {
                    Status = OcspRespStatus.MalformedRequest,
                    Error = $"Error when creating OcspReq from the request. Exception: {e.Message}"
                };
            }

            // Validates whether the ocsp request have certificate's requests
            Req[] requests = ocspRequest.GetRequestList();
            if (requests == null || requests.Length == 0)
            {
                return new OcspReqResult
                {
                    Status = OcspRespStatus.MalformedRequest,
                    Error = "Request list is empty"
                };
            }

            
            // Valitates whether the requests are of this CA's responsibility
            X509Certificate issuerCertificate = null;
            X509Certificate[] issuerCerts = System.Linq.Enumerable.ToArray((await OcspResponderRepository.GetIssuerCertificates()));



            Req[] list = ocspRequest.GetRequestList();
            for (var i = 0; i < list.Length; i++)
            {
                Req request = list[i];
                CertificateID certificateId = request.GetCertID();
                X509Certificate recognizedIssuerCertificate = System.Linq.Enumerable.SingleOrDefault(issuerCerts, issuerCert => certificateId.MatchesIssuer(issuerCert));

                if (i == 0)
                {
                    issuerCertificate = recognizedIssuerCertificate;
                }

                if (recognizedIssuerCertificate == null || !Equals(recognizedIssuerCertificate, issuerCertificate))
                {
                    return new OcspReqResult
                    {
                        Status = OcspRespStatus.Unauthorized,
                        Error = "Any certificate is not of this CA's responsibility"
                    };
                }

                issuerCertificate = recognizedIssuerCertificate;
            }

            // Validation passed so we return the ocspRequest with success status
            return new OcspReqResult
            {
                Status = OcspRespStatus.Successful,
                OcspRequest = ocspRequest,
                IssuerCertificate = issuerCertificate
            };
        }

        /// <summary>
        /// Creates the <see cref="OcspReq"/> from <see cref="OcspHttpRequest"/>
        /// </summary>
        /// <param name="httpRequest"><see cref="OcspHttpRequest"/></param>
        /// <returns><see cref="OcspReq"/></returns>
        private OcspReq CreateOcspReqFromHttpRequest(OcspHttpRequest httpRequest)
        {
            switch (httpRequest.HttpMethod.ToUpper())
            {
                case "GET":
                    return CreateOcspReqFromGet(httpRequest);
                case "POST":
                    return CreateOcspReqFromPost(httpRequest);
                default:
                    throw new System.Net.Http.HttpRequestException("Only GET and POST methods are allowed");
            }
        }

        /// <summary>
        /// Creates the <see cref="OcspReq"/> from POST
        /// </summary>
        /// <param name="httpRequest"><see cref="OcspHttpRequest"/></param>
        /// <returns><see cref="OcspReq"/></returns>
        private OcspReq CreateOcspReqFromPost(OcspHttpRequest httpRequest)
        {
            return new OcspReq(httpRequest.Content);
        }

        /// <summary>
        /// Creates the <see cref="OcspReq"/> from GET/>
        /// </summary>
        /// <param name="httpRequest"><see cref="OcspHttpRequest"/></param>
        /// <returns><see cref="OcspReq"/></returns>
        private OcspReq CreateOcspReqFromGet(OcspHttpRequest httpRequest)
        {
            string[] pathSegments = httpRequest.RequestUri.Segments;
            string lastSegment = pathSegments[pathSegments.Length - 1];

            string encodedOcspRequest = System.Web.HttpUtility.UrlDecode(lastSegment);
            byte[] bytes = System.Convert.FromBase64String(encodedOcspRequest);

            return new OcspReq(bytes);
        }

        /// <summary>
        /// Creates a <see cref="OcspHttpResponse"/> including the ocsp response as byte array
        /// </summary>
        /// <param name="ocspResponseBytes">ocsp response as byte array</param>
        /// <returns><see cref="OcspHttpResponse"/></returns>
        private OcspHttpResponse CreateResponse(byte[] ocspResponseBytes)
        {
            return new OcspHttpResponse(ocspResponseBytes, "application/ocsp-response", System.Net.HttpStatusCode.OK);
        }

        private static OCSPRespGenerator OcspResponseGenerator { get; } = new OCSPRespGenerator();

        /// <inheritdoc cref="IBcOcspResponderRepository"/>
        private IBcOcspResponderRepository OcspResponderRepository { get; }

        /// <inheritdoc cref="IOcspLogger"/>
        private IOcspLogger Log { get; }

        public OcspResponder(IOcspResponderRepository ocspResponderRepository, IOcspLogger logger)
        {
            OcspResponderRepository = new BcOcspResponderRepositoryAdapter(ocspResponderRepository);
            Log = logger;
        }
    }
}