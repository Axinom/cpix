using Axinom.Cpix.Internal;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class RecipientCollection : EntityCollection<Recipient>
	{
		public const string ContainerXmlElementName = "DeliveryDataList";

		internal RecipientCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => ContainerXmlElementName;

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, Recipient entity)
		{
			var recipientRsa = entity.Certificate.GetRSAPublicKey();

			// Ensure that we have the document-scoped cryptographic material available.
			if (Document.DocumentKey == null)
				Document.GenerateKeys();

			var encryptedDocumentKey = recipientRsa.Encrypt(Document.DocumentKey, RSAEncryptionPadding.OaepSHA1);
			var encryptedMacKey = recipientRsa.Encrypt(Document.MacKey, RSAEncryptionPadding.OaepSHA1);

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

			return XmlHelpers.AppendChildAndReuseNamespaces(element, container);
		}

		protected override Recipient DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			// First, just extract the X.509 certificate. It must exist or we will consider the delivery data invalid.
			var certificateNode = element.SelectSingleNode("cpix:DeliveryKey/ds:X509Data/ds:X509Certificate", namespaces);

			if (certificateNode == null)
				throw new InvalidCpixDataException("Found a delivery data element with no X.509 certificate embedded. This is not supported.");

			var certificate = new X509Certificate2(Convert.FromBase64String(certificateNode.InnerText));

			// If we do not already have the document secrets available, try to load them.
			if (Document.DocumentKey == null)
			{
				var matchingRecipientCertificate = Document.RecipientCertificates.FirstOrDefault(c => c.Thumbprint == certificate.Thumbprint);

				if (matchingRecipientCertificate != null)
				{
					// Yes, we have a delivery key! Use this delivery key to load the delivery data.
					var deliveryData = XmlHelpers.Deserialize<DeliveryDataElement>(element);
					deliveryData.LoadTimeValidate();

					var rsa = matchingRecipientCertificate.GetRSAPrivateKey();
					var macKey = rsa.Decrypt(deliveryData.MacMethod.Key.CipherData.CipherValue, RSAEncryptionPadding.OaepSHA1);
					var documentKey = rsa.Decrypt(deliveryData.DocumentKey.Data.Secret.EncryptedValue.CipherData.CipherValue, RSAEncryptionPadding.OaepSHA1);

					Document.ImportKeys(documentKey, macKey);
				}
			}

			return new Recipient(certificate);
		}

		protected override void ValidateCollectionStateBeforeAdd(Recipient entity)
		{
			base.ValidateCollectionStateBeforeAdd(entity);

			if (AllItems.Any(i => i.Certificate == entity.Certificate || i.Certificate.Thumbprint == entity.Certificate.Thumbprint))
				throw new InvalidOperationException("The collection already contains a recipient identified by the same certificate.");

			// If there were no recipients before and we just added one, this means that keys will from now on be encrypted.
			// We thus need to make sure that all keys are new keys - loaded keys that remain clear are not tolerable!
			if (!AllItems.Any() && Document.ContentKeys.LoadedItems.Any())
				throw new InvalidOperationException("You cannot add a recipient to a CPIX document that contains loaded clear content keys. If you wish to encrypt all such keys, you must first remove and re-add them to the document to signal that intent.");
		}

		internal override void ValidateCollectionStateBeforeSave()
		{
			base.ValidateCollectionStateBeforeSave();

			// If there are no recipients but the document contains loaded encrypted content keys, they will remain encrypted.
			if (!AllItems.Any() && Document.ContentKeys.LoadedItems.Any(key => key.IsLoadedEncryptedKey))
				throw new InvalidOperationException("You cannot remove all recipients from a CPIX document that contains loaded encrypted content keys. If you wish to convert all such keys to clear keys, you must first remove and re-add them to the document to signal that intent.");
		}
	}
}
