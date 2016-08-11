using System;
using System.Security;

namespace Axinom.Cpix
{
	/// <summary>
	/// Thown when an attempt is made to use a certificate that is associated with weak cryptographic parameters.
	/// </summary>
	[Serializable]
	public class WeakCertificateException : SecurityException
	{
		public WeakCertificateException() { }
		public WeakCertificateException(string message) : base(message) { }
		public WeakCertificateException(string message, Exception inner) : base(message, inner) { }
		protected WeakCertificateException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
