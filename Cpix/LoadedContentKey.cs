using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A content key loaded from an existing CPIX document. The value may or may not be decryptable
	/// but even if not, the content key can always be saved as-is without corrupting any signatures.
	/// </summary>
	sealed class LoadedContentKey : IContentKey
	{
		public Guid Id { get; }

		/// <summary>
		/// The clear (unencrypted) value of the content key. This must consist of 16 bytes of data if specified.
		/// Null if the value is not available (e.g. because it was loaded from file and you have no keys to decrypt it).
		/// </summary>
		public byte[] Value { get; }

		public LoadedContentKey(Guid id, byte[] value)
		{
			Id = id;
			Value = value;
		}
	}
}
