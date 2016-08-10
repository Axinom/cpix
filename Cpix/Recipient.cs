using System;
using System.Security.Cryptography.X509Certificates;

namespace Axinom.Cpix
{
	/// <summary>
	/// An identity that is authorized to access the content keys of a CPIX document.
	/// </summary>
	public sealed class Recipient : Entity
	{
		/// <summary>
		/// A certificate identifying the recipient and the asymmetric key used to secure communications.
		/// </summary>
		public X509Certificate2 Certificate { get; }

		public Recipient(X509Certificate2 certificate)
		{
			if (certificate == null)
				throw new ArgumentNullException(nameof(certificate));

			Certificate = certificate;
		}

		internal override void ValidateNewEntity(CpixDocument document)
		{
			CryptographyHelpers.ValidateRecipientCertificateAndPublicKey(Certificate);
		}

		internal override void ValidateLoadedEntity(CpixDocument document)
		{
			// We do not particularly care if someone has, with highly questionable intent, used weak certificates
			// in their CPIX documented generated via other mechanisms. We won't use them ourselves but we can ignore them here.
		}
	}
}
