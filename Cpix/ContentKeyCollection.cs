using Axinom.Cpix.DocumentModel;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class ContentKeyCollection : EntityCollection<IContentKey, ContentKey>
	{
		public ContentKeyCollection(CpixDocument document) : base(document)
		{
		}

		protected override string ContainerName => "ContentKeyList";

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

			if (_document.Recipients.Any())
			{
				// We have to encrypt the key. Okay. Ensure we have the crypto values available.
				if (_document.DocumentKey == null)
					_document.GenerateKeys();

				// Unique IV is generated for every content key.
				var iv = new byte[128 / 8];
				_document.Random.GetBytes(iv);

				var aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = _document.DocumentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.PKCS7,
					IV = iv
				};

				var mac = new HMACSHA512(_document.MacKey);

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

		protected override void ValidateCollectionStateBeforeAdd(ContentKey entity)
		{
			if (this.Any(item => item.Id == entity.Id))
				throw new InvalidOperationException("The collection already contains a content key with the same ID.");

			if (!_document.ContentKeysAreReadable)
				throw new InvalidOperationException("New content keys cannot be added to a loaded CPIX document that contains encrypted content keys if you do not possess a delivery key.");

			base.ValidateCollectionStateBeforeAdd(entity);
		}
	}
}
