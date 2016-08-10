using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// Thrown when it is not possible to resolve a content key for a context key context.
	/// </summary>
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
