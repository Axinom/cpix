using System.Security.Cryptography;

namespace Axinom.Cpix.Compatibility
{
	/// <summary>
	/// SignatureDescription impl for http://www.w3.org/2001/04/xmldsig-more#rsa-sha512
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
			CryptoConfig.AddAlgorithm(typeof(RSAPKCS1SHA512SignatureDescription), Name);
		}

		/// <summary>
		/// .NET calls this parameterless ctor
		/// </summary>
		public RSAPKCS1SHA512SignatureDescription()
		{
			KeyAlgorithm = "System.Security.Cryptography.RSACryptoServiceProvider";
			DigestAlgorithm = "System.Security.Cryptography.SHA512Managed";
			FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
			DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
		}

		public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
		{
			var asymmetricSignatureDeformatter = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(DeformatterAlgorithm);
			asymmetricSignatureDeformatter.SetKey(key);
			asymmetricSignatureDeformatter.SetHashAlgorithm("SHA512");
			return asymmetricSignatureDeformatter;
		}

		public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
		{
			var asymmetricSignatureFormatter = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(FormatterAlgorithm);
			asymmetricSignatureFormatter.SetKey(key);
			asymmetricSignatureFormatter.SetHashAlgorithm("SHA512");
			return asymmetricSignatureFormatter;
		}
	}
}
