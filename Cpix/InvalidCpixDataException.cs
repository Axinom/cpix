using System;

namespace Axinom.Cpix
{
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
