
namespace TestOCSP
{

    public enum CertificateStatus { 
        Good = 0, 
        Revoked = 1, 
        Unknown = 2 
    };


    public class OcspClient
    {


        private Org.BouncyCastle.Asn1.Asn1OctetString? m_nonceAsn1OctetString;


        /// <summary>
        /// Method that checks the status of a certificate
        /// </summary>
        /// <param name="eeCert"></param>
        /// <param name="issuerCert"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async System.Threading.Tasks.Task< byte[]> QueryBinary(Org.BouncyCastle.X509.X509Certificate endEntityCert, Org.BouncyCastle.X509.X509Certificate issuerCert, string url)
        {
            Org.BouncyCastle.Ocsp.OcspReq req = GenerateOcspRequest(issuerCert, endEntityCert.SerialNumber);

            // byte[] binaryResp = PostData(url, req.GetEncoded(), "application/ocsp-request", "application/ocsp-response");
            byte[] binaryResp = await PostDataAsync( url, req.GetEncoded(), "application/ocsp-request", "application/ocsp-response");

            return binaryResp;
        } // End Task QueryBinary 


        /// <summary>
        /// Returns the URL of the OCSP server that contains the certificate
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public string? GetAuthorityInformationAccessOcspUrl(Org.BouncyCastle.X509.X509Certificate cert)
        {
            System.Collections.Generic.List<string> ocspUrls = new System.Collections.Generic.List<string>();

            try
            {
                // AuthorityInfoAccess.Id: 1.3.6.1.5.5.7.1.1
                Org.BouncyCastle.Asn1.Asn1Object? obj = GetExtensionValue(cert, Org.BouncyCastle.Asn1.X509.X509Extensions.AuthorityInfoAccess.Id);

                if (obj == null)
                    return null;

                // Switched to manual parse 
                Org.BouncyCastle.Asn1.Asn1Sequence s = (Org.BouncyCastle.Asn1.Asn1Sequence)obj;
                System.Collections.IEnumerator elements = s.GetEnumerator();

                while (elements.MoveNext())
                {
                    Org.BouncyCastle.Asn1.Asn1Sequence element = (Org.BouncyCastle.Asn1.Asn1Sequence)elements.Current;
                    Org.BouncyCastle.Asn1.DerObjectIdentifier oid = (Org.BouncyCastle.Asn1.DerObjectIdentifier)element[0];

                    if (oid.Id.Equals("1.3.6.1.5.5.7.48.1")) // Is Ocsp? 
                    {
                        Org.BouncyCastle.Asn1.Asn1TaggedObject taggedObject = (Org.BouncyCastle.Asn1.Asn1TaggedObject)element[1];
                        Org.BouncyCastle.Asn1.X509.GeneralName gn = (Org.BouncyCastle.Asn1.X509.GeneralName)Org.BouncyCastle.Asn1.X509.GeneralName.GetInstance(taggedObject);
                        ocspUrls.Add(((Org.BouncyCastle.Asn1.DerIA5String)Org.BouncyCastle.Asn1.DerIA5String.GetInstance(gn.Name)).GetString());
                    } // End if (oid.Id.Equals("1.3.6.1.5.5.7.48.1")) // Is Ocsp?  

                } // Whend 
            }
            catch (System.Exception)
            {
                return null;
            }

            if(ocspUrls.Count > 0)
                return ocspUrls[0];

            return null;
        } // End Function GetAuthorityInformationAccessOcspUrl 


        /// <summary>
        /// Processes the response from the OCSP server and returns the certificate status
        /// </summary>
        /// <param name="binaryResp"></param>
        /// <returns></returns>
        public CertificateStatus ProcessOcspResponse(byte[] binaryResp)
        {
            if (binaryResp.Length == 0)
                return CertificateStatus.Unknown;

            Org.BouncyCastle.Ocsp.OcspResp r = new Org.BouncyCastle.Ocsp.OcspResp(binaryResp);
            CertificateStatus cStatus = CertificateStatus.Unknown;

            if (r.Status == Org.BouncyCastle.Ocsp.OcspRespStatus.Successful)
            {
                Org.BouncyCastle.Ocsp.BasicOcspResp or = (Org.BouncyCastle.Ocsp.BasicOcspResp)r.GetResponseObject();

                // The nonce is a randomly generated value that should be unique for each OCSP request.
                // If an attacker tries to use an old OCSP response for a new request, the nonce will differ,
                // and the client will notice that the nonce in the response does not match the one it sent.
                // This prevents replay attacks, where an old response could be reused maliciously.
                if (or.GetExtensionValue(Org.BouncyCastle.Asn1.Ocsp.OcspObjectIdentifiers.PkixOcspNonce).ToString() !=
                    this.m_nonceAsn1OctetString!.ToString())
                    throw new System.Exception("Bad nonce value");

                if (or.Responses.Length == 1)
                {
                    Org.BouncyCastle.Ocsp.SingleResp resp = or.Responses[0];

                    object certificateStatus = resp.GetCertStatus();

                    if (certificateStatus == Org.BouncyCastle.Ocsp.CertificateStatus.Good)
                    {
                        cStatus = CertificateStatus.Good;
                    }
                    else if (certificateStatus is Org.BouncyCastle.Ocsp.RevokedStatus)
                    {
                        cStatus = CertificateStatus.Revoked;
                    }
                    else if (certificateStatus is Org.BouncyCastle.Ocsp.UnknownStatus)
                    {
                        cStatus = CertificateStatus.Unknown;
                    }

                } // End if (or.Responses.Length == 1) 

            } // End if (r.Status == Org.BouncyCastle.Ocsp.OcspRespStatus.Successful) 
            else
            {
                throw new System.Exception("Unknow status '" + r.Status + "'.");
            }

            return cStatus;
        } // End Function ProcessOcspResponse 


#if NEED_SYNCHRONOUS


        /// <summary>
        /// Builds the web request and returns the result of it
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <returns></returns>
        private byte[] PostData(string url, byte[] data, string contentType, string accept)
        {
            byte[] resp;

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = data.Length;
            request.Accept = accept;
            using (System.IO.Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);

                using (System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse())
                {
                    using (System.IO.Stream respStream = response.GetResponseStream())
                    {
                        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                        {
                            respStream.CopyTo(ms);
                            resp = ms.ToArray();
                            respStream.Close();
                        } // End Using ms 

                    } // End Using respStream 

                } // End Using response 

                stream.Close();
            } // End Using stream 

            return resp;
        } // End Function PostData 

#endif


        private async System.Threading.Tasks.Task<byte[]> PostDataAsync(string url, byte[] data, string contentType, string accept)
        {
            byte[] ret;

            using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
            {
                using (System.Net.Http.ByteArrayContent requestContent = new System.Net.Http.ByteArrayContent(data))
                {
                    // Set the content type and accept headers
                    requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(accept));

                    // Send POST request
                    using (System.Net.Http.HttpResponseMessage response = await httpClient.PostAsync(url, requestContent))
                    {
                        // Ensure the response is successful
                        response.EnsureSuccessStatusCode();

                        // Read and return the response content as a byte array
                        ret = await response.Content.ReadAsByteArrayAsync();
                    } // End Using response 

                } // End Using requestContent 

            } // End Using httpClient 

            return ret;
        } // End Task PostDataAsync 


        protected static Org.BouncyCastle.Asn1.Asn1Object? GetExtensionValue(
                Org.BouncyCastle.X509.X509Certificate cert,
                string oid
            )
        {
            if (cert == null)
                return null;

            Org.BouncyCastle.Asn1.Asn1OctetString ocspExtension = cert.GetExtensionValue(new Org.BouncyCastle.Asn1.DerObjectIdentifier(oid));
            if (ocspExtension == null)
                return null;

            byte[] bytes = ocspExtension.GetOctets();

            if (bytes == null)
                return null;

            Org.BouncyCastle.Asn1.Asn1Object obj;
            using (Org.BouncyCastle.Asn1.Asn1InputStream aIn = new Org.BouncyCastle.Asn1.Asn1InputStream(bytes))
            {
                obj = aIn.ReadObject();
            } // End Using aIn 

            return obj;
        } // End Function GetExtensionValue 


        private Org.BouncyCastle.Ocsp.OcspReq GenerateOcspRequest(Org.BouncyCastle.X509.X509Certificate issuerCert, Org.BouncyCastle.Math.BigInteger serialNumber)
        {
            // Create an AlgorithmIdentifier for SHA-1
            Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier digestAlgorithm = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
                Org.BouncyCastle.Asn1.Oiw.OiwObjectIdentifiers.IdSha1
            );

            Org.BouncyCastle.Ocsp.CertificateID id = new Org.BouncyCastle.Ocsp.CertificateID(
                digestAlgorithm,
                issuerCert, 
                serialNumber
            );
            
            return GenerateOcspRequest(id);
        } // End Function GenerateOcspRequest 


        private Org.BouncyCastle.Ocsp.OcspReq GenerateOcspRequest(Org.BouncyCastle.Ocsp.CertificateID id)
        {
            Org.BouncyCastle.Ocsp.OcspReqGenerator ocspRequestGenerator = new Org.BouncyCastle.Ocsp.OcspReqGenerator();

            ocspRequestGenerator.AddRequest(id);

            System.Collections.Generic.List<Org.BouncyCastle.Asn1.DerObjectIdentifier> oids = new System.Collections.Generic.List<Org.BouncyCastle.Asn1.DerObjectIdentifier>();
            System.Collections.Generic.Dictionary<Org.BouncyCastle.Asn1.DerObjectIdentifier, Org.BouncyCastle.Asn1.X509.X509Extension> values =
                new System.Collections.Generic.Dictionary<Org.BouncyCastle.Asn1.DerObjectIdentifier, Org.BouncyCastle.Asn1.X509.X509Extension>();

            oids.Add(Org.BouncyCastle.Asn1.Ocsp.OcspObjectIdentifiers.PkixOcspNonce);

            this.m_nonceAsn1OctetString = new Org.BouncyCastle.Asn1.DerOctetString(
                new Org.BouncyCastle.Asn1.DerOctetString(Org.BouncyCastle.Math.BigInteger.ValueOf(System.DateTime.Now.Ticks).ToByteArray())
            );

            values.Add(Org.BouncyCastle.Asn1.Ocsp.OcspObjectIdentifiers.PkixOcspNonce, new Org.BouncyCastle.Asn1.X509.X509Extension(false, this.m_nonceAsn1OctetString));
            ocspRequestGenerator.SetRequestExtensions(new Org.BouncyCastle.Asn1.X509.X509Extensions(oids, values));

            return ocspRequestGenerator.Generate();
        } // End Function GenerateOcspRequest 


    } // End Class OcspClient 


} // End Namespace FirmaXadesNet.Clients 
