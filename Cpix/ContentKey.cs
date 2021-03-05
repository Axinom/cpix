using System;
using System.Linq;

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
		/// Gets or sets the Common Encryption protection scheme that the content key
		/// is intended to be used with. When set, the value shall be a 4-character
		/// protection scheme name, one of "cenc", "cens", "cbc1", "cbcs". If omitted,
		/// then content may be encrypted using any Common Encryption protection scheme.
		/// </summary>
		public string CommonEncryptionScheme { get; set; }

		/// <summary>
		/// Gets whether the content key is a loaded encrypted content key.
		/// </summary>
		internal bool IsLoadedEncryptedKey { get; set; }

		internal override void ValidateLoadedEntity(CpixDocument document)
		{
			ValidateEntity(document);

			// We skip length check if we do not have a value for an encrypted key (it will be read-only).
			if (IsLoadedEncryptedKey && Value != null && Value.Length != Constants.ContentKeyLengthInBytes)
				throw new InvalidCpixDataException($"A {Constants.ContentKeyLengthInBytes}-byte value must be provided for each new content key.");
		}

		internal override void ValidateNewEntity(CpixDocument document)
		{
			ValidateEntity(document);

			if (Value == null || Value.Length != Constants.ContentKeyLengthInBytes)
				throw new InvalidCpixDataException($"A {Constants.ContentKeyLengthInBytes}-byte value must be provided for each new content key.");
		}

		private void ValidateEntity(CpixDocument document)
		{
			// Contains validation code common for both loaded and new entities.
			// This private method shall be called from the ValidateLoadedEntity()
			// and ValidateNewEntity() methods.

			if (Id == Guid.Empty)
				throw new InvalidCpixDataException("A unique key ID must be provided for each content key.");

			if (ExplicitIv != null && ExplicitIv.Length != Constants.ContentKeyExplicitIvLengthInBytes)
				throw new InvalidCpixDataException($"The explicit IVs associated with content keys must be {Constants.ContentKeyExplicitIvLengthInBytes} byte long.");

			if (CommonEncryptionScheme != null && !Constants.ValidCommonEncryptionSchemes.Contains(CommonEncryptionScheme))
			{
				throw new InvalidCpixDataException(
					$"The Common Encryption protection scheme associated with content keys must be one of" +
					$"{string.Join(", ", Constants.ValidCommonEncryptionSchemes.Select(x => $"'{x}'"))}.");
			}
		}
	}
}
