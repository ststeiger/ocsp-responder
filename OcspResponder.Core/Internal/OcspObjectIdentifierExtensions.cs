
namespace OcspResponder.Core.Internal
{

    /// <summary>
    /// Extensions for OcspObjectIdentifiers
    /// </summary>
    internal class OcspObjectIdentifierExtensions 
        : Org.BouncyCastle.Asn1.Ocsp.OcspObjectIdentifiers
    {
        public static readonly Org.BouncyCastle.Asn1.DerObjectIdentifier PkixOscpPrefSigAlgs = 
            new Org.BouncyCastle.Asn1.DerObjectIdentifier(PkixOcsp + ".8");

        public static readonly Org.BouncyCastle.Asn1.DerObjectIdentifier PkixOcspExtendedRevoke = 
            new Org.BouncyCastle.Asn1.DerObjectIdentifier(PkixOcsp + ".9");
    }
}
