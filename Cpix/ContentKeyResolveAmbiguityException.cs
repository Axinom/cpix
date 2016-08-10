using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// Thrown when resolving a content key for a content key context results in ambiguity.
	/// </summary>
	[Serializable]
	public class ContentKeyResolveAmbiguityException : ContentKeyResolveException
	{
		public ContentKeyResolveAmbiguityException() { }
		public ContentKeyResolveAmbiguityException(string message) : base(message) { }
		public ContentKeyResolveAmbiguityException(string message, Exception inner) : base(message, inner) { }
		protected ContentKeyResolveAmbiguityException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
