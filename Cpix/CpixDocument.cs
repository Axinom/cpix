using Axinom.Cpix.Compatibility;
using Axinom.Cpix.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Schema;
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
	/// * signatures on all content key assignment rules
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
		/// Certificates of the identities whose signature is present on all the content key assignment rules.
		/// To add more signatures use <see cref="AddUsageRuleSignature(X509Certificate2)"/>.
		/// </summary>
		public IReadOnlyCollection<X509Certificate2> UsageRulesSignedBy => _loadedRuleSignatures.Select(tuple => tuple.Item2).Concat(_addedRuleSigners).ToArray();

		/// <summary>
		/// Certificate of the identity whose signature is present on the entire document.
		/// To create or re-create this signatur use <see cref="SetDocumentSignature(X509Certificate2)"/>.
		/// </summary>
		public X509Certificate2 DocumentSignedBy => _desiredDocumentSigner;

		/// <summary>
		/// The set of content keys present in the CPIX document.
		/// To add more content keys, use <see cref="AddContentKey(ContentKey)"/>.
		/// </summary>
		public IReadOnlyCollection<IContentKey> ContentKeys => _contentKeys;

		/// <summary>
		/// The set of content key assignment rules present in the CPIX document.
		/// To add more content key assignment rules, use <see cref="AddUsageRule(UsageRule)"/>.
		/// </summary>
		public IReadOnlyCollection<IUsageRule> UsageRules => _loadedRules.Concat(_addedRules).ToArray();

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

			if (_contentKeys.Any(key => key.Id == contentKey.Id))
				throw new ArgumentException("The CPIX document already contains a content key with the same ID.");

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
		/// Adds a content key assignment rule to the document.
		/// 
		/// Only available if there are no existing signatures that cover key assignment rules in their scope.
		/// You must remove any signatures that cover key assignment rules before you can add new items to the document.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown if there are signatures that include content key assignment rules in their scope.
		/// </exception>
		public void AddUsageRule(UsageRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			// Even though existing signatures may only cover existing rules (and not get invalidated by adding new ones),
			// we still require them to be removed first because signatures only covering some rules are a crime against nature.

			if (_loadedRuleSignatures.Count != 0)
				throw new InvalidOperationException("You must remove (and optionally re-apply) any assignment-rule-scope signatures before adding new content key assignment rules.");

			if (_loadedDocumentSigner != null)
				throw new InvalidOperationException("You must remove (and optionally re-apply) any document-scope signatures before adding new content key assignment rules.");

			rule.Validate(_contentKeys);

			_addedRules.Add(rule);
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
		/// Adds a signature over all the assignment rules in the document.
		/// You must remove or re-apply any document signature before you can add signatures to assignment rules.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if there are no content key assignment rules.</exception>
		/// <exception cref="InvalidOperationException">Thrown if there is an untouched document signature.</exception>
		public void AddUsageRuleSignature(X509Certificate2 signingCertificate)
		{
			if (signingCertificate == null)
				throw new ArgumentNullException(nameof(signingCertificate));

			if (_loadedDocumentSigner != null)
				throw new InvalidOperationException("You must remove (and optionally re-apply) any document-scope signatures before adding new content key assignment rule signatures.");

			if (UsageRules.Count == 0)
				throw new InvalidOperationException("You cannot add a signature over content key assignment rules if no assignment rules exist in the CPIX document.");

			ValidateSigningCertificate(signingCertificate);

			_addedRuleSigners.Add(signingCertificate);
		}

		/// <summary>
		/// Removes all signatures that are scoped to key assignment rules.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if there is an untouched document signature.</exception>
		public void RemoveUsageRuleSignatures()
		{
			if (_loadedDocumentSigner != null)
				throw new InvalidOperationException("You must remove (and optionally re-apply) any document-scope signatures before removing content key assignment rule signatures.");

			foreach (var signature in _loadedRuleSignatures)
			{
				// Remove signature from XML document.
				signature.Item1.ParentNode.RemoveChild(signature.Item1);
			}

			// And then forget them all.
			_loadedRuleSignatures.Clear();
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

			if (_loadedDocumentSignature != null)
			{
				_loadedDocumentSignature.ParentNode.RemoveChild(_loadedDocumentSignature);
				_loadedDocumentSignature = null;

				// Signals that old signature is no longer meaningful. Whatever is in "desired" counts.
				_loadedDocumentSigner = null;
			}

			_desiredDocumentSigner = signingCertificate;
		}

		// Contains the loaded form of the document. This will be cloned and modified on save to apply any changes.
		private XmlDocument _loadedXml = null;

		// For a loaded document, informative only (signatures are preserved as-is in XML on save).
		// For a new document, later used to generate content keys in XML.
		private List<ContentKey> _contentKeys = new List<ContentKey>();

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
		// Reset to null if re-signing is requested.
		// For a new document, always null.
		private X509Certificate2 _loadedDocumentSigner;

		// For a loaded document, this references the document-level signature.
		// It will be removed from the XML document and set to null if a new document-level signature is to be applied.
		private XmlElement _loadedDocumentSignature;

		// For a loaded document, informative only (rules are preserved as-is in XML on save).
		// For a new document, empty.
		private List<IUsageRule> _loadedRules = new List<IUsageRule>();

		// For a loaded document, empty.
		// For a new document, used to generate rules in XML.
		private List<UsageRule> _addedRules = new List<UsageRule>();

		// For a loaded document, lists existing siantures on rule scope.
		// If re-signing is requested, elements will be removed from XML document and list cleared.
		// If no re-signing is performed, informative only - signatures are preserved as-is in XML on save.
		private List<Tuple<XmlElement, X509Certificate2>> _loadedRuleSignatures = new List<Tuple<XmlElement, X509Certificate2>>();

		// List of new signatures to add, rule-scoped.
		private List<X509Certificate2> _addedRuleSigners = new List<X509Certificate2>();

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

			// If we loaded a file, we do not require a key to be present here as we might not be a recipient.
			foreach (var contentKey in _contentKeys)
				contentKey.Validate(allowNullValue: _loadedXml != null);

			foreach (var rule in _addedRules)
				rule.Validate(_contentKeys);

			// Saving is a multi-phase process:
			// 1) If loaded document, clone loaded document.
			// 2) If new document.
			//		2a) Serialize content keys and (if encrypting keys) delivery data.
			//		2b) Sign content keys (if signing requested).
			// 3) Serialize added key assignment rules and (re-)sign the whole set.
			// 4) If signing document, (re-)sign document.

			XmlDocument document;
			XmlNamespaceManager namespaces;

			if (_loadedXml != null)
			{
				document = (XmlDocument)_loadedXml.CloneNode(true);
				namespaces = CreateNamespaceManager(document);
			}
			else
			{
				document = SerializeContentKeysAndGenerateDeliveryData();
				namespaces = CreateNamespaceManager(document);

				SignContentKeys(document);
			}

			SerializeAddedUsageRules(document);

			SignUsageRules(document);
			SignDocument(document);

			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
			{
				// The serialization we do above results in some duplicate namespaces on the assignment rules.
				// What if the original already has duplicates that are signed?
				// Hrm, might have to remove this if that is the case. But don't worry about it for now.
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
				Indent = true,
				IndentChars = "\t",
				Encoding = Encoding.UTF8
			}))
			{
				document.Save(writer);
			}
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
					KeyId = key.Id,
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
			return XmlObjectToXmlDocument(root);
		}

		private void SerializeAddedUsageRules(XmlDocument document)
		{
			var namespaces = CreateNamespaceManager(document);

			// If there are existing rules, add to the end. Otherwise, add after content keys.
			var insertAfter = document.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyUsageRule[last()]", namespaces);

			if (insertAfter == null)
				insertAfter = document.SelectSingleNode("/cpix:CPIX/cpix:ContentKey[last()]", namespaces);

			foreach (var rule in _addedRules)
			{
				// We do not give them XML IDs, as that is handled later during signing.
				var ruleObject = new UsageRuleElement
				{
					KeyId = rule.KeyId
				};

				if (rule.AudioFilter != null)
				{
					ruleObject.AudioFilter = new AudioFilterElement
					{
						MinChannels = rule.AudioFilter.MinChannels,
						MaxChannels = rule.AudioFilter.MaxChannels
					};
				}

				if (rule.BitrateFilter != null)
				{
					ruleObject.BitrateFilter = new BitrateFilterElement
					{
						MinBitrate = rule.BitrateFilter.MinBitrate,
						MaxBitrate = rule.BitrateFilter.MaxBitrate
					};
				}

				if (rule.LabelFilter != null)
				{
					ruleObject.LabelFilter = new LabelFilterElement
					{
						Label = rule.LabelFilter.Label
					};
				}

				if (rule.VideoFilter != null)
				{
					ruleObject.VideoFilter = new VideoFilterElement
					{
						MinPixels = rule.VideoFilter.MinPixels,
						MaxPixels = rule.VideoFilter.MaxPixels
					};
				}

				var xmlElement = XmlObjectToXmlDocument(ruleObject).DocumentElement;
				var imported = document.ImportNode(xmlElement, true);

				// Insert and move pointer forward for next one.
				insertAfter = document.DocumentElement.InsertAfter(imported, insertAfter);
			}
		}

		private void SignUsageRules(XmlDocument document)
		{
			if (_addedRuleSigners.Count == 0)
				return;

			var namespaces = CreateNamespaceManager(document);

			// If we are signing content key assignment rules, we re-number them all and assign unique IDs to each.
			// This way we can be assured of easy and straightforward identification for purposes of signature references.
			int ruleNumber = 1;

			foreach (XmlElement ruleElement in document.SelectNodes("/cpix:CPIX/cpix:ContentKeyUsageRule", namespaces))
				ruleElement.SetAttribute("id", $"UsageRule{ruleNumber++}");

			var ruleIdUris = TryDetermineUsageRuleUniqueIdUris(document);

			foreach (var signer in _addedRuleSigners)
			{
				using (var signingKey = signer.GetRSAPrivateKey())
				{
					var signedXml = new SignedXml(document)
					{
						SigningKey = signingKey
					};

					// Add each content key assignment rule element as a reference to sign.
					foreach (var uri in ruleIdUris)
					{
						var whatToSign = new Reference
						{
							Uri = uri,

							// A nice strong algorithm without known weaknesses that are easily exploitable.
							DigestMethod = Constants.Sha512Algorithm
						};

						// Just some arbitrary transform. It... works.
						whatToSign.AddTransform(new XmlDsigC14NTransform());

						signedXml.AddReference(whatToSign);
					}

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					signedXml.SignedInfo.SignatureMethod = RSAPKCS1SHA512SignatureDescription.Name;

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

		private void SignContentKeys(XmlDocument document)
		{
			var contentKeyIdUris = TryDetermineContentKeyUniqueIdUris(document);

			foreach (var signer in _contentKeySigners)
			{
				using (var signingKey = signer.GetRSAPrivateKey())
				{
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
							DigestMethod = Constants.Sha512Algorithm
						};

						// Just some arbitrary transform. It... works.
						whatToSign.AddTransform(new XmlDsigC14NTransform());

						signedXml.AddReference(whatToSign);
					}

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					signedXml.SignedInfo.SignatureMethod = RSAPKCS1SHA512SignatureDescription.Name;

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
			if (_desiredDocumentSigner == null)
				return;

			using (var signingKey = _desiredDocumentSigner.GetRSAPrivateKey())
			{
				var signedXml = new SignedXml(document)
				{
					SigningKey = signingKey
				};

				var whatToSign = new Reference
				{
					// The entire document is signed.
					Uri = "",

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					DigestMethod = Constants.Sha512Algorithm
				};

				// This signature (and other signatures) are inside the signed data, so exclude them.
				whatToSign.AddTransform(new XmlDsigEnvelopedSignatureTransform());

				signedXml.AddReference(whatToSign);

				// A nice strong algorithm without known weaknesses that are easily exploitable.
				signedXml.SignedInfo.SignatureMethod = RSAPKCS1SHA512SignatureDescription.Name;

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

			var settings = new XmlReaderSettings
			{
				ValidationType = ValidationType.Schema
			};
			settings.Schemas.Add(_schemaSet);

			var document = new XmlDocument();
			document.Load(XmlReader.Create(stream, settings));

			if (document.DocumentElement?.Name != "CPIX" || document.DocumentElement?.NamespaceURI != Constants.CpixNamespace)
				throw new InvalidCpixDataException("The provided XML file does not appear to be a CPIX document - the name of the root element is incorrect.");

			cpix._loadedXml = document;

			// 1) If signatures exist, verify them all.
			//		1a) If any signatures match the "known" scopes, categorize them accordingly.
			VerifyAndCategorizeSignatures(document, cpix);

			// 2) If delivery data exists, attempt to find delivery data we can work with for decryption.
			// 3) Load content keys, decrypting if needed and if we have the document key.
			LoadContentKeys(document, cpix, decryptionCertificates ?? new X509Certificate2[0]);

			// 4) Load key assignment rules.
			LoadUsageRules(document, cpix);

			// 5) Validate.
			if (cpix.ContentKeys.Count == 0)
				throw new InvalidCpixDataException("There were no content keys in the CPIX document.");

			if (cpix.ContentKeys.Count != cpix.ContentKeys.Select(key => key.Id).Distinct().Count())
				throw new InvalidCpixDataException("There were duplicate content keys in the CPIX document.");

			return cpix;
		}

		private static void VerifyAndCategorizeSignatures(XmlDocument document, CpixDocument cpix)
		{
			var namespaces = CreateNamespaceManager(document);

			var contentKeyIdUris = TryDetermineContentKeyUniqueIdUris(document);
			var ruleIdUris = TryDetermineUsageRuleUniqueIdUris(document);

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
				else if (ruleIdUris != null && referenceUris.OrderBy(x => x).SequenceEqual(ruleIdUris.OrderBy(x => x)))
				{
					// This is a signature over all content key assignment rules.
					cpix._loadedRuleSignatures.Add(new Tuple<XmlElement, X509Certificate2>(signature, certificate));
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
				var macKey = rsa.Decrypt(deliveryData.MacMethod.Key.CipherData.CipherValue, true);
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

				var loadedContentKey = new ContentKey
				{
					Id = contentKey.KeyId,
					Value = value
				};

				// If we are not able to decrypt the value, we accept a null value.
				loadedContentKey.Validate(allowNullValue: value == null);

				cpix._contentKeys.Add(loadedContentKey);
			}
		}

		private static void LoadUsageRules(XmlDocument document, CpixDocument cpix)
		{
			var namespaces = CreateNamespaceManager(document);

			foreach (XmlElement ruleNode in document.SelectNodes("/cpix:CPIX/cpix:ContentKeyUsageRule", namespaces))
			{
				var element = XmlElementToXmlDeserialized<UsageRuleElement>(ruleNode);

				var rule = new UsageRule
				{
					KeyId = element.KeyId
				};

				if (element.AudioFilter != null)
				{
					rule.AudioFilter = new AudioFilter
					{
						MinChannels = element.AudioFilter.MinChannels,
						MaxChannels = element.AudioFilter.MaxChannels
					};
				}

				if (element.BitrateFilter != null)
				{
					rule.BitrateFilter = new BitrateFilter
					{
						MinBitrate = element.BitrateFilter.MinBitrate,
						MaxBitrate = element.BitrateFilter.MaxBitrate
					};
				}
				
				if (element.LabelFilter != null)
				{
					rule.LabelFilter = new LabelFilter
					{
						Label = element.LabelFilter.Label
					};
				}

				if (element.VideoFilter != null)
				{
					rule.VideoFilter = new VideoFilter
					{
						MinPixels = element.VideoFilter.MinPixels,
						MaxPixels = element.VideoFilter.MaxPixels
					};
				}

				rule.Validate(cpix.ContentKeys);

				cpix._loadedRules.Add(rule);
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

			// If there are no items, there is nothing to reference.
			if (result.Count == 0)
				return null;

			// We got all the IDs. But are they unique?
			if (result.Distinct().Count() != result.Count)
				return null; // Nope!

			// The IDs also need to be unique between the content keys and all other XML elements!
			// The XML digital signature implementation must verifiy that no such funny business takes
			// place when verifying the signatures, so no need to worry extra about that.

			return result.ToArray();
		}

		/// <summary>
		/// Returns the refrence URIs (in the XML Digital Signature sense) of all content key assignment rule elements
		/// OR null if the assignment rule elements in the CPIX document cannot be uniquely identified for signing purposes.
		/// </summary>
		private static string[] TryDetermineUsageRuleUniqueIdUris(XmlDocument document)
		{
			var namespaces = CreateNamespaceManager(document);

			var result = new List<string>();

			foreach (XmlElement rule in document.SelectNodes("/cpix:CPIX/cpix:ContentKeyUsageRule", namespaces))
			{
				var id = rule.GetAttribute("id");

				if (id == "")
				{
					// Missing is empty value - it is not possible to uniquely identify this rule.
					return null;
				}

				result.Add("#" + id);
			}

			// If there are no items, there is nothing to reference.
			if (result.Count == 0)
				return null;

			// We got all the IDs. But are they unique?
			if (result.Distinct().Count() != result.Count)
				return null; // Nope!

			// The IDs also need to be unique between the rules and all other XML elements!
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

		private static XmlDocument XmlObjectToXmlDocument<T>(T xmlObject)
		{
			using (var intermediateXmlBuffer = new MemoryStream())
			{
				var serializer = new XmlSerializer(typeof(T));
				serializer.Serialize(intermediateXmlBuffer, xmlObject);

				// Seek back to beginning to load contents into XmlDocument.
				intermediateXmlBuffer.Position = 0;

				var xmlDocument = new XmlDocument();
				xmlDocument.Load(intermediateXmlBuffer);

				return xmlDocument;
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

		/// <summary>
		/// Finds a suitable content key to samples that conform to the provided sample description.
		/// The content key assignment rules are resolved and exactly one match is assumed. No match
		/// or multiple matches are considered an error condition (ambiguities are not allowed).
		/// </summary>
		public IContentKey ResolveContentKey(SampleDescription sampleDescription)
		{
			if (sampleDescription == null)
				throw new ArgumentNullException(nameof(sampleDescription));

			sampleDescription.Validate();

			var results = UsageRules
				.Where(rule => EvaluateUsageRule(rule, sampleDescription))
				.Select(rule => ContentKeys.Single(key => key.Id == rule.KeyId))
				.ToArray();

			if (results.Length == 0)
				throw new ContentKeyResolveException("No content keys were assigned to samples matching the provided sample description.");

			if (results.Length > 1)
				throw new ContentKeyResolveException("Multiple content keys were assigned to samples matching the provided sample description.");

			return results[0];
		}

		private static bool EvaluateUsageRule(IUsageRule rule, SampleDescription sampleDescription)
		{
			if (rule.LabelFilter != null)
			{
				if (sampleDescription.Labels == null || !sampleDescription.Labels.Any(label => label == rule.LabelFilter.Label))
					return false;
			}

			if (rule.VideoFilter != null)
			{
				if (sampleDescription.Type != SampleType.Video)
					return false;

				if (rule.VideoFilter.MinPixels != null && !(sampleDescription.PicturePixelCount >= rule.VideoFilter.MinPixels))
					return false;
				if (rule.VideoFilter.MaxPixels != null && !(sampleDescription.PicturePixelCount <= rule.VideoFilter.MaxPixels))
					return false;
			}

			if (rule.AudioFilter != null)
			{
				if (sampleDescription.Type != SampleType.Audio)
					return false;

				if (rule.AudioFilter.MinChannels != null && !(sampleDescription.AudioChannelCount >= rule.AudioFilter.MinChannels))
					return false;
				if (rule.AudioFilter.MaxChannels != null && !(sampleDescription.AudioChannelCount <= rule.AudioFilter.MaxChannels))
					return false;
			}

			if (rule.BitrateFilter != null)
			{
				if (rule.BitrateFilter.MinBitrate != null && !(sampleDescription.Bitrate >= rule.BitrateFilter.MinBitrate))
					return false;
				if (rule.BitrateFilter.MaxBitrate != null && !(sampleDescription.Bitrate <= rule.BitrateFilter.MaxBitrate))
					return false;
			}

			return true;
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

		static CpixDocument()
		{
			// For compatibility with .NET Framework < 4.6.2.
			// Remove once 4.6.2 is published and can be safely targeted.
			RSAPKCS1SHA512SignatureDescription.Register();

			// Load the XSD for CPIX and all the referenced schemas.
			_schemaSet = new XmlSchemaSet();

			var assembly = Assembly.GetExecutingAssembly();

			// At least one of the schemas contains DTDs, so let's say it's okay to process them.
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse
			};

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.xenc-schema.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				_schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.xmldsig-core-schema.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				_schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.pskc.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				_schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.cpix.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				_schemaSet.Add(null, reader);

			_schemaSet.Compile();
		}

		private static readonly XmlSchemaSet _schemaSet;

		private static readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();
	}
}
