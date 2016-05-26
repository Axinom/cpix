using System.Security.Cryptography;

namespace Axinom.Cpix
{
	static class Constants
	{
		public const string CpixNamespace = "urn:dashif:org:cpix";
		public const string PskcNamespace = "urn:ietf:params:xml:ns:keyprov:pskc";
		public const string XmlDigitalSignatureNamespace = "http://www.w3.org/2000/09/xmldsig#";
		public const string XmlEncryptionNamespace = "http://www.w3.org/2001/04/xmlenc#";

		public const string Aes256CbcAlgorithm = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
		public const string ContentKeyAlgorithm = "TODO";

		public const string HmacSha512Algorithm = "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512";
		public const string RsaOaepAlgorithm = "http://www.w3.org/2001/04/xmlenc#rsa-oaep-mgf1p";

		public const int MinimumRsaKeySizeInBits = 2048;

		public static readonly string Sha1Oid = CryptoConfig.MapNameToOID("SHA1");

		public const string Sha512Algorithm = "http://www.w3.org/2001/04/xmlenc#sha512";
	}
}
