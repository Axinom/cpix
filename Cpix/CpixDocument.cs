using Axinom.Cpix.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Serialization;

namespace Axinom.Cpix
{
	/// <summary>
	/// A CPIX document, either created anew for saving or loaded from file for reading and/or partial modifying.
	/// </summary>
	/// <remarks>
	/// All content keys are automatically encrypted on save and delivery data generated based on provided certificates.
	/// Content keys can only be added/defined on initial document creation. You can modify other parts of the document
	/// (e.g. key assignment rules) and save it again later, though, even if you have no access to the decryption keys.
	/// 
	/// To keep things managable, there are three basic types of digital signatures supported by this implementation:
	/// * signatures on all content keys
	/// * signatures on all key assignment rules
	/// * signature on the entire document (only one allowed; also covers other signatures in signed data!)
	/// 
	/// Any signatures that do not match the above signed data sets are ignored on load.
	/// You must have the private keys to re-sign any of these parts that you modify.
	/// </remarks>
	public sealed class CpixDocument
	{
		/// <summary>
		/// Certificates identifying all the intended recipients of the CPIX document (entities that are given access
		/// to the document key). To add more recipients, use <see cref="AddRecipient(X509Certificate2)"/>.
		/// 
		/// If this list contains any recipients, the content keys are encrypted.
		/// If this list is empty, content keys are delivered in the clear.
		/// </summary>
		public IReadOnlyCollection<X509Certificate2> Recipients => _recipients;

		/// <summary>
		/// Certificates of the identities whose signature is present on all the content keys.
		/// To add more signatures use <see cref="AddContentKeySignature(X509Certificate2)"/>.
		/// </summary>
		public IReadOnlyCollection<X509Certificate2> ContentKeysSignedBy => _contentKeySigners;

		/// <summary>
		/// Certificate of the identity whose signature is present on the entire document.
		/// To create or re-create this signatur use <see cref="SetDocumentSignature(X509Certificate2)"/>.
		/// </summary>
		public X509Certificate2 DocumentSignedBy => _desiredDocumentSigner ?? _loadedDocumentSigner;

		/// <summary>
		/// The set of content keys present in the CPIX document.
		/// To add more content keys, use <see cref="AddContentKey(ContentKey)"/>.
		/// </summary>
		public IReadOnlyCollection<IContentKey> ContentKeys => _contentKeys;

		/// <summary>
		/// Whether the values of content keys are available.
		/// 
		/// This is always true for new documents. This may be false for loaded documents if the content keys
		/// were encrypted and none of our decryption certificates were among the listed recipients.
		/// </summary>
		public bool ContentKeysAvailable { get; private set; } = true;

		/// <summary>
		/// Adds a content key to the document.
		/// Only available if this is a newly created document.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if this is a loaded document.</exception>
		public void AddContentKey(ContentKey contentKey)
		{
			if (contentKey == null)
				throw new ArgumentNullException(nameof(contentKey));

			if (_loadedXml != null)
				throw new InvalidOperationException("You cannot add content keys to a loaded CPIX document, only to a newly created one.");

			contentKey.Validate();

			_contentKeys.Add(contentKey);
		}

		/// <summary>
		/// Adds a recipient to the document.
		/// Only available if this is a newly created document.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if this is a loaded document.</exception>
		public void AddRecipient(X509Certificate2 recipient)
		{
			if (recipient == null)
				throw new ArgumentNullException(nameof(recipient));

			if (_loadedXml != null)
				throw new InvalidOperationException("You cannot add recipients to a loaded CPIX document, only to a newly created one.");

			ValidateRecipientCertificate(recipient);

			_recipients.Add(recipient);
		}

		/// <summary>
		/// Adds a signature over all the content keys in the document.
		/// Only available if this is a newly created document.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if this is a loaded document.</exception>
		/// <remarks>
		/// The limitations on signing only at initial creation are not so much technical but to actually help
		/// enforce good security practices - if you sign the data, you should actually be the one who generated it.
		/// </remarks>
		public void AddContentKeySignature(X509Certificate2 signingCertificate)
		{
			if (signingCertificate == null)
				throw new ArgumentNullException(nameof(signingCertificate));

			if (_loadedXml != null)
				throw new InvalidOperationException("You cannot add content key signatures to a loaded CPIX document, only to a newly created one.");

			ValidateSigningCertificate(signingCertificate);

			_contentKeySigners.Add(signingCertificate);
		}

		/// <summary>
		/// Creates, recreates or removes a signature over the entire document.
		/// Set to null to remove a document signature or to a certificate to add/replace one.
		/// </summary>
		/// <remarks>
		/// There may only be a single document signature and any change to the document will invalidate it.
		/// </remarks>
		public void SetDocumentSignature(X509Certificate2 signingCertificate)
		{
			if (signingCertificate != null)
				ValidateSigningCertificate(signingCertificate);

			_desiredDocumentSigner = signingCertificate;
		}

		// Contains the loaded form of the document. This will be cloned and modified on save to apply any changes.
		private XmlDocument _loadedXml = null;

		// For a loaded document, informative only (signatures are preserved as-is in XML on save).
		// For a loaded document, items will be of type LoadedContentKey.
		// For a new document, later used to generate content keys in XML.
		// For a new document, items will be of type ContentKey.
		private List<IContentKey> _contentKeys = new List<IContentKey>();

		// For a loaded document, informative only (delivery data is preserved as-is in XML on save).
		// For a new document, later used to generate deliery data in XML.
		private List<X509Certificate2> _recipients = new List<X509Certificate2>();

		// For a loaded document, informative only (signatures are preserved as-is in XML on save).
		// For a new document, later used to generate signatures in XML.
		private List<X509Certificate2> _contentKeySigners = new List<X509Certificate2>();

		// For a loaded document, equal to _loadedDocumentSigner.
		// For a new document, used to generate the signature in XML.
		private X509Certificate2 _desiredDocumentSigner;

		// For a loaded document, informative only (signatures are preserved as-is in XML on save).
		// For a new document, always null.
		private X509Certificate2 _loadedDocumentSigner;

		// For a loaded document, this references the document-level signature.
		// On save, it will be removed if a new document-level signature is to be applied.
		private XmlElement _loadedDocumentSignature;

		/// <summary>
		/// Saves the CPIX document to a stream.
		/// </summary>
		public void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			// Do some basic validation.
			if (_contentKeys.Count == 0)
				throw new InvalidOperationException("Cannot save a CPIX document without any content keys.");

			// Saving is a multi-phase process:
			// 1) If loaded document
			//		1a) Clone loaded document.
			//		1b) If re-signing requested, remove existing document-level signatures.
			// 2) If new document.
			//		2a) Serialize content keys and (if encrypting keys) delivery data.
			//		2b) Sign content keys (if signing requested).
			// 3) TODO: Serialize key assignment rules and sign them.
			// 4) If signing or re-signing document, sign document.

			XmlDocument document;
			XmlNamespaceManager namespaces;

			if (_loadedXml != null)
			{
				document = (XmlDocument)_loadedXml.CloneNode(true);
				namespaces = CreateNamespaceManager(document);

				// TODO: Remove document signatures once we need to implement that.
			}
			else
			{
				document = SerializeContentKeysAndGenerateDeliveryData();
				namespaces = CreateNamespaceManager(document);

				SignContentKeys(document);
			}

			SignDocument(document);

			document.Save(stream);
		}

		/// <summary>
		/// Creates the basic structure of a new document, consisting of the content keys and delivery data.
		/// </summary>
		private XmlDocument SerializeContentKeysAndGenerateDeliveryData()
		{
			var root = new DocumentRootElement();

			// If null, no encryption is used. when serializing content key elements 
			// If non-null, encryption is used when serializing content key elements.
			AesManaged aes = null;
			HMACSHA512 mac = null;

			if (_recipients.Count != 0)
			{
				// There are recipients specified, so we shall encrypt the data.
				// First, generate all the keys and provide the delivery data.

				// 256-bit key is desirable.
				var documentKey = new byte[256 / 8];
				_random.GetBytes(documentKey);

				// Initialize AES with the document key.
				// A unique IV will be generated later for every content key.
				aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = documentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.None
				};

				// 512-bit HMAC key is desirable.
				var macKey = new byte[512 / 8];
				_random.GetBytes(macKey);

				mac = new HMACSHA512(macKey);

				// Generate delivery data for each recipient.
				foreach (var recipient in _recipients)
				{
					var recipientRsa = ((RSACryptoServiceProvider)recipient.PublicKey.Key);

					var encryptedDocumentKey = recipientRsa.Encrypt(documentKey, true);
					var encryptedMacKey = recipientRsa.Encrypt(macKey, true);

					root.DeliveryData.Add(new DeliveryDataElement
					{
						DeliveryKey = new DeliveryKeyElement
						{
							X509Data = new X509Data
							{
								Certificate = recipient.GetRawCertData()
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
						MacKey = new MacKey
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
					});
				}
			}

			// Just to assign unique document-level ID to each content key element.
			int contentKeyNumber = 1;

			foreach (var key in _contentKeys)
			{
				var element = new ContentKeyElement
				{
					XmlId = $"ContentKey{contentKeyNumber++}",
					Algorithm = Constants.ContentKeyAlgorithm,
					KeyId = key.Id.ToString(),
					Data = new DataElement
					{
						Secret = new SecretDataElement()
					}
				};

				if (aes == null)
				{
					// Keys are serialized in the clear.
					element.Data.Secret.PlainValue = key.Value;
				}
				else
				{
					// Keys are encrypted with the document key.

					// Unique IV is generated for every content key.
					var iv = new byte[128 / 8];
					_random.GetBytes(iv);

					aes.IV = iv;

					using (var encryptor = aes.CreateEncryptor())
					{
						var encryptedValue = encryptor.TransformFinalBlock(key.Value, 0, key.Value.Length);

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

				root.ContentKeys.Add(element);
			}

			// Now transform this structure into a brand new XmlDocument.
			using (var intermediateXmlBuffer = new MemoryStream())
			{
				var serializer = new XmlSerializer(typeof(DocumentRootElement));
				serializer.Serialize(intermediateXmlBuffer, root);

				// Seek back to beginning to load contents into XmlDocument.
				intermediateXmlBuffer.Position = 0;

				var xmlDocument = new XmlDocument();
				xmlDocument.Load(intermediateXmlBuffer);

				return xmlDocument;
			}
		}

		private void SignContentKeys(XmlDocument document)
		{
			var contentKeyIdUris = TryDetermineContentKeyUniqueIdUris(document);

			foreach (var signer in _contentKeySigners)
			{
				// There is some funny business that happens with certificates loaded from PFX files in .NET 4.6.2 Preview.
				// You can't use them with the RSA-SHA512 algorithm! It just complains about an invalid algorithm.
				// To get around it, simply export and re-import the key pair to a new instance of the RSA CSP.
				using (var signingKey = new RSACryptoServiceProvider())
				{
					var exportedSigningKey = ((RSACryptoServiceProvider)signer.PrivateKey).ExportParameters(true);
					signingKey.ImportParameters(exportedSigningKey);

					var signedXml = new SignedXml(document)
					{
						SigningKey = signingKey
					};

					// Add each content key element as a reference to sign.
					foreach (var uri in contentKeyIdUris)
					{
						var whatToSign = new Reference
						{
							Uri = uri,

							// A nice strong algorithm without known weaknesses that are easily exploitable.
							DigestMethod = SignedXml.XmlDsigSHA512Url
						};

						// Just some arbitrary transform. It... works.
						whatToSign.AddTransform(new XmlDsigC14NTransform());

						signedXml.AddReference(whatToSign);
					}

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA512Url;

					// Canonical XML 1.0 (omit comments); I suppose it works fine, no deep thoughts about this.
					signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl;

					// Signer certificate must be delivered with the signature.
					signedXml.KeyInfo.AddClause(new KeyInfoX509Data(signer));

					// Ready to sign! Let's go!
					signedXml.ComputeSignature();

					// Now stick the Signature element it generated back into the document and we are done.
					var signature = signedXml.GetXml();
					document.DocumentElement.AppendChild(document.ImportNode(signature, true));
				}
			}
		}

		private void SignDocument(XmlDocument document)
		{
			if (_desiredDocumentSigner != _loadedDocumentSigner && _loadedDocumentSigner != null)
			{
				// The desired document signer has changed. Remove old signature from document.
				// Err okay but how do we find it? Remember that we operate on a cloned document tree!

				// ...just look for the same signature value. Should work reasonably well.
				var namespaces = CreateNamespaceManager(document);

				var lookingForSignatureValue = _loadedDocumentSignature.SelectSingleNode("ds:SignatureValue", namespaces).InnerText;

				var documentSignatureNode = (XmlElement)document.SelectNodes("/cpix:CPIX/ds:Signature/ds:SignatureValue", namespaces).Cast<XmlElement>().SingleOrDefault(signatureValueNode => signatureValueNode.InnerText == lookingForSignatureValue)?.ParentNode;

				if (documentSignatureNode == null)
					throw new Exception("Internal error: we lost track of the document signature node!");

				documentSignatureNode.ParentNode.RemoveChild(documentSignatureNode);
			}

			if (_desiredDocumentSigner == null)
				return;

			// There is some funny business that happens with certificates loaded from PFX files in .NET 4.6.2 Preview.
			// You can't use them with the RSA-SHA512 algorithm! It just complains about an invalid algorithm.
			// To get around it, simply export and re-import the key pair to a new instance of the RSA CSP.
			using (var signingKey = new RSACryptoServiceProvider())
			{
				var exportedSigningKey = ((RSACryptoServiceProvider)_desiredDocumentSigner.PrivateKey).ExportParameters(true);
				signingKey.ImportParameters(exportedSigningKey);

				var signedXml = new SignedXml(document)
				{
					SigningKey = signingKey
				};

				var whatToSign = new Reference
				{
					// The entire document is signed.
					Uri = "",

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					DigestMethod = SignedXml.XmlDsigSHA512Url
				};

				// This signature (and other signatures) are inside the signed data, so exclude them.
				whatToSign.AddTransform(new XmlDsigEnvelopedSignatureTransform());

				signedXml.AddReference(whatToSign);

				// A nice strong algorithm without known weaknesses that are easily exploitable.
				signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA512Url;

				// Canonical XML 1.0 (omit comments); I suppose it works fine, no deep thoughts about this.
				signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl;

				// Signer certificate must be delivered with the signature.
				signedXml.KeyInfo.AddClause(new KeyInfoX509Data(_desiredDocumentSigner));

				// Ready to sign! Let's go!
				signedXml.ComputeSignature();

				// Now stick the Signature element it generated back into the document and we are done.
				var signature = signedXml.GetXml();
				document.DocumentElement.AppendChild(document.ImportNode(signature, true));
			}
		}

		/* TODO: Make this required before any modifications once we actually support modifications.
		 * 
		/// <summary>
		/// Removes all document signatures present. You must do this before saving the document, since any changes
		/// will invalidate existing document signatures and the document as a whole must therefore be re-signed on save.
		/// </summary>
		public void RemoveDocumentSignatures()
		{
			throw new NotImplementedException();
		}
		*/

		/// <summary>
		/// Loads a CPIX document from a stream, decrypting it using the key pairs of the provided certificates, if required.
		/// 
		/// All top-level signatures are verified. Note that a valid signature does not mean that the signer
		/// is trusted! It is the caller's responsibility to ensure that any signers are trusted!
		/// </summary>
		public static CpixDocument Load(Stream stream, IReadOnlyCollection<X509Certificate2> decryptionCertificates = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (decryptionCertificates != null)
				foreach (var decryptionCertificate in decryptionCertificates)
					ValidateDecryptionCertificate(decryptionCertificate);

			// We will fill this instance with the loaded data.
			var cpix = new CpixDocument();

			var document = new XmlDocument();
			document.Load(stream);

			cpix._loadedXml = document;

			// 1) If signatures exist, verify them all.
			//		1a) If any signatures match the "known" scopes, categorize them accordingly.
			VerifyAndCategorizeSignatures(document, cpix);

			// 2) If delivery data exists, attempt to find delivery data we can work with for decryption.
			// 3) Load content keys, decrypting if needed and if we have the document key.
			LoadContentKeys(document, cpix, decryptionCertificates ?? new X509Certificate2[0]);

			// 4) TODO: Load key assignment rules.

			// 5) Validate.
			if (cpix.ContentKeys.Count == 0)
				throw new NotSupportedException("There were no content keys in the CPIX document.");

			return cpix;
		}

		private static void VerifyAndCategorizeSignatures(XmlDocument document, CpixDocument cpix)
		{
			var namespaces = CreateNamespaceManager(document);

			var contentKeyIdUris = TryDetermineContentKeyUniqueIdUris(document);

			foreach (XmlElement signature in document.SelectNodes("/cpix:CPIX/ds:Signature", namespaces))
			{
				var signedXml = new SignedXml(document);
				signedXml.LoadXml(signature);

				// We verify all signatures using the data embedded within them.
				if (!signedXml.CheckSignature())
					throw new SecurityException("CPIX signature failed to verify - the document has been tampered with!");

				// The signature must include a certificate for the signer in order to be categorized.
				var certificateElement = signature.SelectSingleNode("ds:KeyInfo/ds:X509Data/ds:X509Certificate", namespaces);

				if (certificateElement == null)
					continue;

				var certificate = new X509Certificate2(Convert.FromBase64String(certificateElement.InnerText));

				var referenceUris = signedXml.SignedInfo.References.Cast<Reference>().Select(r => r.Uri).ToArray();

				// The signature must have a recognizable scope in order to be categorized.
				if (referenceUris.Length == 1 && referenceUris.Single() == "")
				{
					// This is a document-level signature.
					cpix._loadedDocumentSigner = certificate;
					cpix._desiredDocumentSigner = certificate;
					cpix._loadedDocumentSignature = signature;
				}
				else if (contentKeyIdUris != null && referenceUris.OrderBy(x => x).SequenceEqual(contentKeyIdUris.OrderBy(x => x)))
				{
					// This is a signature over all content keys.
					cpix._contentKeySigners.Add(certificate);
				}
				else
				{
					// The scope is strange! Not one of the standard scopes. We will not categorize this.
					// The signature will be preserved on save but no promises about it remaining valid!
				}
			}
		}

		private static void LoadContentKeys(XmlDocument document, CpixDocument cpix, IReadOnlyCollection<X509Certificate2> decryptionCertificates)
		{
			var namespaces = CreateNamespaceManager(document);

			// If content keys are encrypted AND we can decrypt them, we fill these and use for later cryptography.
			AesManaged aes = null;
			HMACSHA512 mac = null;

			// Now look for and acquire the document key so that the encrypted data can be decrypted.
			// If there is no delivery data (no encryption) or we do not have a decryption certificate
			// then we load the key metadata but not the actual bytes of the content key.
			var deliveryDataNodes = document.SelectNodes("/cpix:CPIX/cpix:DeliveryData", namespaces);

			bool isEncrypted = deliveryDataNodes.Count != 0;

			foreach (XmlElement deliveryDataNode in deliveryDataNodes)
			{
				// The delivery key must contain our X509 certificate or we will consider it a non-match.
				var deliveryCertificateNode = deliveryDataNode.SelectSingleNode("cpix:DeliveryKey/ds:X509Data/ds:X509Certificate", namespaces);

				if (deliveryCertificateNode == null)
					continue; // Huh? Okay, whatever. Ignore it.

				var deliveryCertificate = new X509Certificate2(Convert.FromBase64String(deliveryCertificateNode.InnerText));

				// List all found certificates as recipients.
				cpix._recipients.Add(deliveryCertificate);

				var decryptionCertificate = decryptionCertificates.FirstOrDefault(c => c.Thumbprint == deliveryCertificate.Thumbprint);

				if (decryptionCertificate == null)
					continue; // Nope. Next, please.

				// This delivery data is for us!
				// Deserialize this DeliveryData into a nice structure for easier processing.
				var deliveryData = XmlElementToXmlDeserialized<DeliveryDataElement>(deliveryDataNode);

				// Verify that all the values make sense.
				deliveryData.LoadTimeValidate();

				var rsa = (RSACryptoServiceProvider)decryptionCertificate.PrivateKey;
				var macKey = rsa.Decrypt(deliveryData.MacKey.Key.CipherData.CipherValue, true);
				var documentKey = rsa.Decrypt(deliveryData.DocumentKey.Data.Secret.EncryptedValue.CipherData.CipherValue, true);

				aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = documentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.None
				};

				mac = new HMACSHA512(macKey);

				// Found a matching set of delivery data, no need to try the remaining data.
				break;
			}

			// Content keys are available if either there is no encryption of if we have the document key.
			cpix.ContentKeysAvailable = !isEncrypted || aes != null;

			// Preparations complete. Let's now load the content keys!
			foreach (XmlElement contentKeyNode in document.SelectNodes("/cpix:CPIX/cpix:ContentKey", namespaces))
			{
				// Deserialize to data structure for easier processing.
				var contentKey = XmlElementToXmlDeserialized<ContentKeyElement>(contentKeyNode);

				contentKey.LoadTimeValidate();

				// Start loading the data.
				var keyId = Guid.Parse(contentKey.KeyId);

				byte[] value = null;

				if (isEncrypted && contentKey.HasPlainValue)
					throw new NotSupportedException("A plain content key was found but delivery data was defined. Malformed CPIX?");

				if (contentKey.HasEncryptedValue && aes != null)
				{
					// The value is encrypted and we have the key.
					var calculatedMac = mac.ComputeHash(contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue);

					if (!calculatedMac.SequenceEqual(contentKey.Data.Secret.ValueMAC))
						throw new SecurityException("MAC validation failed - the content key value has been tampered with!");

					var iv = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Take(128 / 8).ToArray();
					var encryptedKey = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Skip(128 / 8).ToArray();

					aes.IV = iv;

					byte[] key;

					using (var decryptor = aes.CreateDecryptor())
					{
						key = decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
					}

					value = key;
				}
				else if (contentKey.HasPlainValue)
				{
					value = contentKey.Data.Secret.PlainValue;
				}

				cpix._contentKeys.Add(new LoadedContentKey(keyId, value));
			}
		}

		/// <summary>
		/// Returns the refrence URIs (in the XML Digital Signature sense) of all content key elements
		/// OR null if content key elements in the CPIX document cannot be uniquely identified for signing purposes.
		/// </summary>
		private static string[] TryDetermineContentKeyUniqueIdUris(XmlDocument document)
		{
			var namespaces = CreateNamespaceManager(document);

			var result = new List<string>();

			foreach (XmlElement contentKey in document.SelectNodes("/cpix:CPIX/cpix:ContentKey", namespaces))
			{
				var id = contentKey.GetAttribute("id");

				if (id == "")
				{
					// Missing is empty value - it is not possible to uniquely identify this content key.
					return null;
				}

				result.Add("#" + id);
			}

			// We got all the IDs. But are they unique?
			if (result.Distinct().Count() != result.Count)
				return null; // Nope!

			// The IDs also need to be unique between the content keys and all other XML elements!
			// The XML digital signature implementation must verifiy that no such funny business takes
			// place when verifying the signatures, so no need to worry extra about that.

			return result.ToArray();
		}

		private static T XmlElementToXmlDeserialized<T>(XmlElement element)
		{
			using (var buffer = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(buffer))
					element.WriteTo(writer);

				buffer.Position = 0;

				var serializer = new XmlSerializer(typeof(T));
				return (T)serializer.Deserialize(buffer);
			}
		}

		private static XmlNamespaceManager CreateNamespaceManager(XmlDocument document)
		{
			var manager = new XmlNamespaceManager(document.NameTable);
			manager.AddNamespace("cpix", Constants.CpixNamespace);
			manager.AddNamespace("pskc", Constants.PskcNamespace);
			manager.AddNamespace("enc", Constants.XmlEncryptionNamespace);
			manager.AddNamespace("ds", Constants.XmlDigitalSignatureNamespace);

			return manager;
		}

		private static void ValidateRecipientCertificate(X509Certificate2 recipient)
		{
			if (recipient.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			var rsaKey = recipient.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for encryption.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		private static void ValidateSigningCertificate(X509Certificate2 signingCertificate)
		{
			if (signingCertificate.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			if (!signingCertificate.HasPrivateKey)
				throw new ArgumentException("The private key of the supplied signing certificate is not available .");

			var rsaKey = signingCertificate.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for signing.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		private static void ValidateDecryptionCertificate(X509Certificate2 decryptionCertificate)
		{
			if (decryptionCertificate.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			if (!decryptionCertificate.HasPrivateKey)
				throw new ArgumentException("The private key of the supplied deryption certificate is not available .");

			var rsaKey = decryptionCertificate.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for signing.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		private static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	}
}
