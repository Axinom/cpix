using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// Thrown when resolving content keys is impossible.
	/// </summary>
	[Serializable]
	public class ContentKeyResolveImpossibleException : ContentKeyResolveException
	{
		public ContentKeyResolveImpossibleException() { }
		public ContentKeyResolveImpossibleException(string message) : base(message) { }
		public ContentKeyResolveImpossibleException(string message, Exception inner) : base(message, inner) { }
		protected ContentKeyResolveImpossibleException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
