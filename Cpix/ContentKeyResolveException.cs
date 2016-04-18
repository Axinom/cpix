using System;

namespace Axinom.Cpix
{
	[Serializable]
	public class ContentKeyResolveException : Exception
	{
		public ContentKeyResolveException() { }
		public ContentKeyResolveException(string message) : base(message) { }
		public ContentKeyResolveException(string message, Exception inner) : base(message, inner) { }
		protected ContentKeyResolveException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
