using Axinom.Cpix.DocumentModel;
using System;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class ContentKeyCollection : EntityCollection<IContentKey, ContentKey>
	{
		public ContentKeyCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => "ContentKeyList";

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, ContentKey entity)
		{
			var element = new ContentKeyElement
			{
				KeyId = entity.Id,
				Data = new DataElement
				{
					Secret = new SecretDataElement()
				}
			};

			if (Document.Recipients.Any())
			{
				// We have to encrypt the key. Okay. Ensure we have the crypto values available.
				if (Document.DocumentKey == null)
					Document.GenerateKeys();

				// Unique IV is generated for every content key.
				var iv = new byte[128 / 8];
				Document.Random.GetBytes(iv);

				var aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = Document.DocumentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.PKCS7,
					IV = iv
				};

				var mac = new HMACSHA512(Document.MacKey);

				using (var encryptor = aes.CreateEncryptor())
				{
					var encryptedValue = encryptor.TransformFinalBlock(entity.Value, 0, entity.Value.Length);

					// NB! We prepend the IV to the value when saving an encrypted value to the document field.
					var fieldValue = iv.Concat(encryptedValue).ToArray();

					element.Data.Secret.EncryptedValue = new EncryptedXmlValue
					{
						CipherData = new CipherDataContainer
						{
							CipherValue = fieldValue
						},
						EncryptionMethod = new EncryptionMethodDeclaration
						{
							Algorithm = Constants.Aes256CbcAlgorithm
						}
					};

					// Never not MAC.
					element.Data.Secret.ValueMAC = mac.ComputeHash(fieldValue);
				}
			}
			else
			{
				// We are saving the key in the clear.
				element.Data.Secret.PlainValue = entity.Value;
			}

			return XmlHelpers.InsertXmlObject(element, document, container);
		}

		protected override ContentKey DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			var contentKey = XmlHelpers.XmlElementToXmlDeserialized<ContentKeyElement>(element);
			contentKey.LoadTimeValidate();

			if (contentKey.HasPlainValue && Document.Recipients.Any())
				throw new InvalidCpixDataException("A content key was delivered in the clear but delivery data was defined. Malformed CPIX!?");

			byte[] value = null;

			if (contentKey.HasEncryptedValue && Document.ContentKeysAreReadable)
			{
				var mac = new HMACSHA512(Document.MacKey);

				var calculatedMac = mac.ComputeHash(contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue);

				if (!calculatedMac.SequenceEqual(contentKey.Data.Secret.ValueMAC))
					throw new SecurityException("MAC validation failed - a content key value has been tampered with!");

				var iv = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Take(128 / 8).ToArray();
				var encryptedKey = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Skip(128 / 8).ToArray();

				var aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = Document.DocumentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.PKCS7,
					IV = iv
				};

				using (var decryptor = aes.CreateDecryptor())
				{
					value = decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
				}
			}
			else if (contentKey.HasPlainValue)
			{
				value = contentKey.Data.Secret.PlainValue;
			}
			else
			{
				// Value is encrpyted and we cannot read it. Nothing to do here.
			}

			return new ContentKey
			{
				Id = contentKey.KeyId,
				Value = value,
				IsExistingEncryptedKey = contentKey.HasEncryptedValue
			};
		}

		protected override void ValidateCollectionStateBeforeAdd(ContentKey entity)
		{
			if (this.Any(key => key.Id == entity.Id))
				throw new InvalidOperationException("The collection already contains a content key with the same ID.");

			if (!Document.ContentKeysAreReadable)
				throw new InvalidOperationException("New content keys cannot be added to a loaded CPIX document that contains encrypted content keys if you do not possess a delivery key.");

			base.ValidateCollectionStateBeforeAdd(entity);
		}
	}
}
