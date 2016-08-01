using System.Security.Cryptography.X509Certificates;

namespace Axinom.Cpix
{
	/// <summary>
	/// An identity that is authorized to access the content keys of a CPIX document.
	/// </summary>
	public interface IRecipient
	{
		/// <summary>
		/// A certificate identifying the recipient and the asymmetric key used to secure communications.
		/// </summary>
		X509Certificate2 Certificate { get; }
	}
}
