using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// Thrown when some loaded or supplied data is not suitable for use in a well-formed CPIX document.
	/// </summary>
	[Serializable]
	public class InvalidCpixDataException : Exception
	{
		public InvalidCpixDataException() { }
		public InvalidCpixDataException(string message) : base(message) { }
		public InvalidCpixDataException(string message, Exception inner) : base(message, inner) { }
		protected InvalidCpixDataException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
