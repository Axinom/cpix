using System;
using System.Security.Cryptography.X509Certificates;

namespace Axinom.Cpix
{
	/// <summary>
	/// An identity that is authorized to access the content keys of a CPIX document.
	/// </summary>
	public sealed class Recipient : IRecipient
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

			// TODO: Any difference for load scenarios? Probably not?
		}
	}
}
