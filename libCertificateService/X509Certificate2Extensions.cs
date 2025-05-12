
namespace libCertificateService
{


    public static class X509Certificate2Extensions
    {


        private static string GetHashAlgorithmFromOid(string signatureAlgorithmOid)
        {
            // Mapping OIDs to hash algorithms (this covers common signature algorithm OIDs)
            switch (signatureAlgorithmOid)
            {
                case "1.2.840.113549.1.1.11": // sha256WithRSAEncryption
                    return "SHA-256";
                case "1.2.840.113549.1.1.12": // sha384WithRSAEncryption
                    return "SHA-384";
                case "1.2.840.113549.1.1.13": // sha512WithRSAEncryption
                    return "SHA-512";


                case "1.2.840.10045.4.3.2": // ecdsa-with-SHA256
                    return "SHA-256";
                case "1.2.840.10045.4.3.3": // ecdsa-with-SHA384
                    return "SHA-384";
                case "1.2.840.10045.4.3.4": // ecdsa-with-SHA512
                    return "SHA-512";


                case "1.2.840.113549.1.1.5": // sha1WithRSAEncryption
                case "1.2.840.10040.4.3": // sha1WithDSA
                    return "SHA-1";


                case "1.2.840.113549.1.1.4":  // RSA is used to encrypt the content and to sign the content hash created by using the MD5 message digest algorithm.
                    return "MD5";
                case "1.2.840.113549.1.1.3":  // RSA is used to encrypt the content and to sign the content hash created by using the MD4 message digest algorithm.
                    return "MD4";
                case "1.2.840.113549.1.1.2":  // RSA is used to encrypt the content and to sign the content hash created by using the MD2 message digest algorithm.
                    return "MD2";

                // Add more OIDs as needed for other algorithms

                default:
                    return "Unknown Hash Algorithm (" + signatureAlgorithmOid + ")";
            } // End Switch 

        } // End Function GetHashAlgorithmFromOid 


        public static string GetHashAlgorithmName(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
        {
            string hashAlgo = GetHashAlgorithmFromOid(certificate.SignatureAlgorithm?.Value ?? "NULL");
            return hashAlgo;
        } // End Function GetHashAlgorithmName 


        private static string GetSignatureAlgorithmFromOid(string signatureAlgorithmOid)
        {
            // Mapping OIDs to encryption algorithms
            switch (signatureAlgorithmOid)
            {
                case "1.2.840.113549.1.1.11": // RSA is used to encrypt the content and to sign the hash created by using the Secure Hashing Algorithm 256 (SHA256) algorithm.
                case "1.2.840.113549.1.1.12": // RSA is used to encrypt the content and to sign the hash created by using the Secure Hashing Algorithm 384 (SHA384) algorithm.
                case "1.2.840.113549.1.1.13": // RSA is used to encrypt the content and to sign the hash created by using the Secure Hashing Algorithm 512 (SHA512) algorithm.
                case "1.2.840.113549.1.1.5":  // RSA is used to encrypt the content and to sign the content hash created by using the Secure Hashing Algorithm 1 (SHA1) algorithm.
                case "1.2.840.113549.1.1.4":  // RSA is used to encrypt the content and to sign the content hash created by using the MD5 message digest algorithm.
                case "1.2.840.113549.1.1.3":  // RSA is used to encrypt the content and to sign the content hash created by using the MD4 message digest algorithm.
                case "1.2.840.113549.1.1.2":  // RSA is used to encrypt the content and to sign the content hash created by using the MD2 message digest algorithm.
                case "1.2.840.113549.1.1.1":  // RSA is used to both encrypt and sign content.
                    return "RSA";

                case "1.2.840.10040.4.3": // dsaWithSHA1
                    return "DSA";

                case "1.2.840.10045.4.3.2": // Elliptic curve Digital Signature Algorithm (DSA) coupled with the Secure Hashing Algorithm (SHA256) algorithm.
                case "1.2.840.10045.4.3.3": // Elliptic curve Digital Signature Algorithm (DSA) coupled with the Secure Hashing Algorithm (SHA384) algorithm.
                case "1.2.840.10045.4.3.4": // Elliptic curve Digital Signature Algorithm (DSA) coupled with the Secure Hashing Algorithm (SHA512) algorithm.
                    return "ECDSA";

                // Add more OIDs for other encryption algorithms as needed
                default:
                    return "Unknown Encryption Algorithm (" + signatureAlgorithmOid + ")";
            } // End Switch 

        } // End Function GetSignatureAlgorithmFromOid 


        public static string GetSignatureAlgorithm(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate
        )
        {
            string signatureAlgorithm = GetSignatureAlgorithmFromOid(certificate.SignatureAlgorithm?.Value ?? "NULL");
            return signatureAlgorithm;
        } // End Function GetSignatureAlgorithm 


        public static System.Numerics.BigInteger GetSerialNumber(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate
        )
        {
            byte[] serialBytes = certificate.GetSerialNumber(); // ) returns the bytes in little-endian order

            System.Numerics.BigInteger serialNumber = new System.Numerics.BigInteger(serialBytes);

            return serialNumber;
        } // End Function GetSerialNumber 


        // Function to parse and extract SAN values from the RawData
        public static System.Collections.Generic.List<string> GetCertificateNetworkNames(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate
        )
        {
            System.Security.Cryptography.X509Certificates.X509Extension? subjectAlternativeNames = certificate.Extensions["2.5.29.17"]; // OID for Subject Alternative Name
            System.Collections.Generic.List<string> ls = GetSubjectAlternativeNames(subjectAlternativeNames?.RawData ?? null);

            string dnsName = certificate.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.DnsName, false);

            if (ls.FindIndex(x => System.StringComparer.InvariantCultureIgnoreCase.Equals(x, dnsName)) == -1)
                ls.Insert(0, dnsName);

            return ls;
        }


        // Function to parse and extract SAN values from the RawData
        public static System.Collections.Generic.List<string> GetSubjectAlternativeNames(
            this System.Security.Cryptography.X509Certificates.X509Certificate2 certificate
        )
        {
            System.Security.Cryptography.X509Certificates.X509Extension? subjectAlternativeNames = certificate.Extensions["2.5.29.17"]; // OID for Subject Alternative Name
            return GetSubjectAlternativeNames(subjectAlternativeNames?.RawData ?? null);
        }


        // Function to parse and extract SAN values from the RawData
        private static System.Collections.Generic.List<string> GetSubjectAlternativeNames(byte[]? rawData)
        {
            System.Collections.Generic.List<string> sanValues = new System.Collections.Generic.List<string>();
            if (rawData == null)
                return sanValues;

            try
            {
                System.Formats.Asn1.Asn1Tag dnsNameTag = new System.Formats.Asn1.Asn1Tag(System.Formats.Asn1.TagClass.ContextSpecific, tagValue: 2, isConstructed: false);
                System.Formats.Asn1.Asn1Tag ipTag = new System.Formats.Asn1.Asn1Tag(System.Formats.Asn1.TagClass.ContextSpecific, tagValue: 7, isConstructed: false);


                // Initialize an AsnValueReader to decode the ASN.1 DER-encoded SAN data
                System.Formats.Asn1.AsnReader asnReader = new System.Formats.Asn1.AsnReader(rawData, System.Formats.Asn1.AsnEncodingRules.DER);
                System.Formats.Asn1.AsnReader sequenceReader = asnReader.ReadSequence();

                while (sequenceReader.HasData)
                {
                    System.Formats.Asn1.Asn1Tag tag = sequenceReader.PeekTag();
                    if (tag == dnsNameTag)
                    {
                        string dnsName = sequenceReader.ReadCharacterString(System.Formats.Asn1.UniversalTagNumber.IA5String, dnsNameTag);

                        if (string.IsNullOrEmpty(dnsName))
                            continue;

                        // System.StringComparer.InvariantCultureIgnoreCase.Equals(x, valueToAdd)
                        if (sanValues.FindIndex(x => System.StringComparer.InvariantCultureIgnoreCase.Equals(x, dnsName)) != -1)
                            sanValues.Add(dnsName);

                        sanValues.Add(dnsName);
                        continue;
                    } // End if (tag == dnsNameTag) 
                    else if (tag == ipTag)
                    {
                        byte[] ipAddressBytes = sequenceReader.ReadOctetString(ipTag);

                        // If the length doesn't match IPv4 or IPv6, log or handle as needed
                        string valueToAdd = "Invalid IP address format";

                        // Convert the byte array to an IP address string (IPv4 or IPv6)
                        if (ipAddressBytes.Length == 4) // IPv4
                        {
                            System.Net.IPAddress address = new System.Net.IPAddress(ipAddressBytes);
                            valueToAdd = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
                        }
                        else if (ipAddressBytes.Length == 16) // IPv6
                        {
                            System.Net.IPAddress address = new System.Net.IPAddress(ipAddressBytes);
                            valueToAdd = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
                        }

                        if (string.IsNullOrEmpty(valueToAdd))
                            continue;

                        if (sanValues.FindIndex(x => System.StringComparer.InvariantCultureIgnoreCase.Equals(x, valueToAdd)) == -1)
                            sanValues.Add(valueToAdd);

                        continue;
                    } // End else if (tag == ipTag) 

                    sequenceReader.ReadEncodedValue();
                } // Whend 

            } // End Try 
            catch (System.Formats.Asn1.AsnContentException e)
            {
                System.Console.WriteLine("Error parsing SAN extension: " + e.Message);
            }

            sanValues.Sort();

            return sanValues;
        } // End Function GetSubjectAlternativeNames 


    } // End Class X509Certificate2Extensions 


} // End Namespace 
