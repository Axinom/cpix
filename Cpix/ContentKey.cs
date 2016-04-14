using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A content key to be added to a CPIX document.
	/// </summary>
	public sealed class ContentKey : IContentKey
	{
		public Guid Id { get; set; }

		/// <summary>
		/// The clear (nonencrypted) value of the content key. Must consist of 128 bits (16 bytes) of data.
		/// </summary>
		public byte[] Value { get; set; }

		/// <summary>
		/// Validates the data before it is accepted for serialization.
		/// </summary>
		internal void Validate()
		{
			if (Id == Guid.Empty)
				throw new NotSupportedException("Content key must have an ID.");

			if (Value?.Length != 16)
				throw new NotSupportedException("Content key must have a 16-byte value.");
		}
	}
}
