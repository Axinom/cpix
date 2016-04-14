using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a single content key that is part of a CPIX document.
	/// </summary>
	public interface IContentKey
	{
		Guid Id { get; }

		/// <summary>
		/// The clear (unencrypted) value of the content key. This must consist of 16 bytes of data if specified.
		/// Null if the value is not available (e.g. because it was loaded from file and you have no keys to decrypt it).
		/// </summary>
		byte[] Value { get; }
	}
}
