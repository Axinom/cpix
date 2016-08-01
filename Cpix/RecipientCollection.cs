using Axinom.Cpix.DocumentModel;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class RecipientCollection : EntityCollection<IRecipient, Recipient>
	{
		internal RecipientCollection(CpixDocument document) : base(document)
		{
		}

		protected override string ContainerName => "DeliveryDataList";

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, Recipient entity)
		{
			var recipientRsa = entity.Certificate.GetRSAPublicKey();

			// Ensure that we have the document-scoped cryptographic material available.
			if (_document.DocumentKey == null)
				_document.GenerateKeys();

			var encryptedDocumentKey = recipientRsa.Encrypt(_document.DocumentKey, RSAEncryptionPadding.OaepSHA1);
			var encryptedMacKey = recipientRsa.Encrypt(_document.MacKey, RSAEncryptionPadding.OaepSHA1);

			var element = new DeliveryDataElement
			{
				DeliveryKey = new DeliveryKeyElement
				{
					X509Data = new X509Data
					{
						Certificate = entity.Certificate.GetRawCertData()
					}
				},
				DocumentKey = new DocumentKeyElement
				{
					Algorithm = Constants.Aes256CbcAlgorithm,
					Data = new DataElement
					{
						Secret = new SecretDataElement
						{
							EncryptedValue = new EncryptedXmlValue
							{
								EncryptionMethod = new EncryptionMethodDeclaration
								{
									Algorithm = Constants.RsaOaepAlgorithm
								},
								CipherData = new CipherDataContainer
								{
									CipherValue = encryptedDocumentKey
								}
							}
						}
					}
				},
				MacMethod = new MacMethodElement
				{
					Algorithm = Constants.HmacSha512Algorithm,
					Key = new EncryptedXmlValue
					{
						EncryptionMethod = new EncryptionMethodDeclaration
						{
							Algorithm = Constants.RsaOaepAlgorithm
						},
						CipherData = new CipherDataContainer
						{
							CipherValue = encryptedMacKey
						}
					}
				}
			};

			return XmlHelpers.InsertXmlObject(element, document, container);
		}

		protected override void ValidateCollectionStateBeforeAdd(Recipient entity)
		{
			base.ValidateCollectionStateBeforeAdd(entity);

			if (AllItems.Any(i => i.Certificate == entity.Certificate || i.Certificate.Thumbprint == entity.Certificate.Thumbprint))
				throw new InvalidOperationException("The collection already contains a recipient identified by the same certificate.");

			// If there were no recipients before and we just added one, this means that keys will from now on be encrypted.
			// We thus need to make sure that all keys are new keys - existing keys that remain clear are not tolerable!
			if (!AllItems.Any() && _document.ContentKeys.ExistingItems.Any())
				throw new InvalidOperationException("You cannot add a recipient to a CPIX document that contains loaded clear content keys. If you wish to encrypt all such keys, you must first remove and re-add them to the document to signal that intent.");
		}

		internal override void ValidateCollectionStateBeforeSave()
		{
			base.ValidateCollectionStateBeforeSave();

			// If there are no recipients but the document contains existing encrypted content keys, they will remain encrypted.
			if (!AllItems.Any() && _document.ContentKeys.ExistingItems.Any(key => key.IsExistingEncryptedKey))
				throw new InvalidOperationException("You cannot remove all recipients from a CPIX document that contains loaded encrypted content keys. If you wish to convert all such keys to clear keys, you must first remove and re-add them to the document to signal that intent.");
		}
	}
}
