using System;

namespace Axinom.Cpix
{
	public sealed class ContentKey : Entity
	{
		/// <summary>
		/// Unique ID of the content key.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the value of the content key. Must be 128 bits (16 bytes) long.
		/// Null if the content key was loaded from a CPIX document and could not be decrypted.
		/// </summary>
		public byte[] Value { get; set; }

		/// <summary>
		/// Get or sets the optional IV that is to be explicitly associated with the
		/// content key. The IV must be 128 bits (16 bytes) long. A common use case
		/// involves FairPlay DRM, where the IV is expected to be transported
		/// together with the content key, instead of being extracted from the
		/// content. Otherwise, the use of this IV is not recommended and it should
		/// be ignored even if set.
		/// </summary>
		public byte[] ExplicitIv { get; set; }

		/// <summary>
		/// Gets whether the content key is a loaded encrypted content key.
		/// </summary>
		internal bool IsLoadedEncryptedKey { get; set; }

		internal override void ValidateLoadedEntity(CpixDocument document)
		{
			if (Id == Guid.Empty)
				throw new InvalidCpixDataException("A unique key ID must be provided for each content key.");

			// We skip length check if we do not have a value for an encrypted key (it will be read-only).
			if (IsLoadedEncryptedKey && Value != null && Value.Length != Constants.ContentKeyLengthInBytes)
				throw new InvalidCpixDataException($"A {Constants.ContentKeyLengthInBytes}-byte value must be provided for each new content key.");

			if (ExplicitIv != null && ExplicitIv.Length != Constants.ContentKeyExplicitIvLengthInBytes)
				throw new InvalidCpixDataException($"The explicit IVs associated with content keys must be {Constants.ContentKeyExplicitIvLengthInBytes} byte long.");
		}

		internal override void ValidateNewEntity(CpixDocument document)
		{
			if (Id == Guid.Empty)
				throw new InvalidCpixDataException("A unique key ID must be provided for each content key.");

			if (Value == null || Value.Length != Constants.ContentKeyLengthInBytes)
				throw new InvalidCpixDataException($"A {Constants.ContentKeyLengthInBytes}-byte value must be provided for each new content key.");

			if (ExplicitIv != null && ExplicitIv.Length != Constants.ContentKeyExplicitIvLengthInBytes)
				throw new InvalidCpixDataException($"The explicit IVs associated with content keys must be {Constants.ContentKeyExplicitIvLengthInBytes} byte long.");
		}
	}
}
