using System.Security.Cryptography;

namespace Axinom.Cpix.Compatibility
{
	/// <summary>
	/// Hack for compatibility with .NET 4.6.1 and older. Remove once 4.6.2 is published and older versions can be dropped.
	/// </summary>
	public sealed class RSAPKCS1SHA512SignatureDescription : SignatureDescription
	{
		public const string Name = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";

		/// <summary>
		/// Registers the http://www.w3.org/2001/04/xmldsig-more#rsa-sha512 algorithm
		/// with the .NET CrytoConfig registry. This needs to be called once per
		/// appdomain before attempting to validate SHA512 signatures.
		/// </summary>
		public static void Register()
		{
			if (CryptoConfig.CreateFromName(Name) == null)
				CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA512SignatureDescription), Name);
		}

		public RSAPKCS1SHA512SignatureDescription()
		{
			KeyAlgorithm = typeof(RSA).FullName;
			DigestAlgorithm = typeof(SHA512Managed).FullName;
			FormatterAlgorithm = typeof(CngSha512RSAPKCS1SignatureFormatter).FullName;
			DeformatterAlgorithm = typeof(RSAPKCS1SignatureDeformatter).FullName;
		}

		public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
		{
			var asymmetricSignatureDeformatter = new RSAPKCS1SignatureDeformatter();
			asymmetricSignatureDeformatter.SetKey(key);
			asymmetricSignatureDeformatter.SetHashAlgorithm("SHA512");
			return asymmetricSignatureDeformatter;
		}

		public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
		{
			var asymmetricSignatureFormatter = new CngSha512RSAPKCS1SignatureFormatter();
			asymmetricSignatureFormatter.SetKey(key);
			asymmetricSignatureFormatter.SetHashAlgorithm("SHA512");
			return asymmetricSignatureFormatter;
		}
	}
}
