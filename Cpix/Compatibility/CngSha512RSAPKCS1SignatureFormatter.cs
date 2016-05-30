using System;
using System.Security.Cryptography;

namespace Axinom.Cpix.Compatibility
{
	/// <summary>
	/// Hack for compatibility with .NET 4.6.1 and older. Remove once 4.6.2 is published and older versions can be dropped.
	/// </summary>
	internal sealed class CngSha512RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter
	{
		private RSACng _rsaKey;

		public CngSha512RSAPKCS1SignatureFormatter() { }

		public CngSha512RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			_rsaKey = (RSACng)key;
		}

		public override void SetKey(AsymmetricAlgorithm key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			_rsaKey = (RSACng)key;
		}

		public override void SetHashAlgorithm(String strName)
		{
		}

		public override byte[] CreateSignature(byte[] rgbHash)
		{
			return ((RSACng)_rsaKey).SignHash(rgbHash, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
		}
	}
}
