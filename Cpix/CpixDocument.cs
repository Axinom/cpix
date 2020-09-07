using Axinom.Cpix.Internal;
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

namespace Axinom.Cpix
{
	/// <summary>
	/// A CPIX document, either created anew for saving or loaded from file for reading and/or modifying.
	/// </summary>
	/// <remarks>
	/// All content keys are automatically encrypted on save and delivery data generated for all recipients.
	/// If no recipients are defined then the content keys are saved in the clear. Converting from one scenario
	/// to the other is fine, but you must then re-add all content keys that were loaded from an existing document.
	/// 
	/// There are two scopes of digital signatures supported by this implementation:
	/// * Any number of signatures may be applied to each entity collection (*List elements in the XML).
	/// * One signature may be applied to the entire document.
	/// 
	/// Any signatures whose scopes do not match the above data sets are validated on load but otherwise ignored,
	/// though validated again on save to ensure that files that fail verification are inadvertently not created.
	/// 
	/// To modify any part of a signed document, you must first remove the signature.
	/// To modify any signed entity collection, you must first remove all signatures on it.
	/// You may immediately re-apply any removed signatures, if you wish, provided that you have the private keys.
	/// 
	/// You must have a delivery key in order to modify a loaded set of content keys. If you do not have a delivery key,
	/// you will be able to see content key metadata and cannot add new content keys to the document.
	/// </remarks>
	public sealed class CpixDocument
	{
		/// <summary>
		/// Gets or sets the content ID, which specifies an identifier for the asset or
		/// content that will be protected by the keys carried in this CPIX document. It is
		/// recommended  to use an identifier that is unique within the scope in which this
		/// document is published.
		/// </summary>
		public string ContentId
		{
			get => _contentId;
			set
			{
				VerifyIsNotReadOnly();
				_contentId = value;
			}
		}

		/// <summary>
		/// Gets the set of recipients that the CPIX document is meant to be securely delivered to.
		/// 
		/// If this collections contains entries, the content keys within the CPIX document are encrypted.
		/// If this collection is empty, the content keys within the CPIX document are not encrypted.
		/// </summary>
		public EntityCollection<Recipient> Recipients { get; }

		/// <summary>
		/// Gets the set of content keys stored in the CPIX document.
		/// </summary>
		public EntityCollection<ContentKey> ContentKeys { get; }

		/// <summary>
		/// Gets the set of usage rules stored in the CPIX document.
		/// </summary>
		public EntityCollection<UsageRule> UsageRules { get; }

		/// <summary>
		/// Gets the set of DRM system signaling entries stored in the CPIX document.
		/// </summary>
		public EntityCollection<DrmSystem> DrmSystems { get; }

		/// <summary>
		/// Gets the set of content key period entries stored in the CPIX document.
		/// </summary>
		public EntityCollection<ContentKeyPeriod> ContentKeyPeriods { get; }

		/// <summary>
		/// Whether the values of content keys are readable.
		/// If false, you can only read the metadata, not the values themselves.
		/// 
		/// This is always true for new documents. This may be false for loaded documents
		/// if the content keys are encrypted and we do not possess any of the delivery keys.
		/// </summary>
		public bool ContentKeysAreReadable => Recipients.LoadedItems.Count() == 0 || DocumentKey != null;

		/// <summary>
		/// Gets whether the document is read-only.
		/// 
		/// This can be the case if you are dealing with a loaded CPIX document that contains a signature that includes the
		/// entire document in scope. You must remove or re-apply the signature to make the document writable.
		/// </summary>
		public bool IsReadOnly => _documentSignature != null;

		/// <summary>
		/// Gets or sets the certificate of the identity that has signed or will sign the document as a whole.
		/// 
		/// You must have the private key of the identity who you set as the signer.
		/// 
		/// Set to null to remove the signature from the document.
		/// </summary>
		public X509Certificate2 SignedBy
		{
			get => _desiredSignedBy;
			set
			{
				if (value != null)
					CryptographyHelpers.ValidateSignerCertificate(value);

				if (_documentSignature != null)
				{
					_documentSignature.Item1.ParentNode.RemoveChild(_documentSignature.Item1);
					_documentSignature = null;
				}

				_desiredSignedBy = value;
			}
		}

		/// <summary>
		/// Finds a suitable content key for a content key context.
		/// 
		/// All content key usage rules are evaluated and exactly one match is expected. No match
		/// or multiple matches are considered an error condition (ambiguities are not allowed).
		/// </summary>
		public ContentKey ResolveContentKey(ContentKeyContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			context.Validate();

			if (UsageRules.Count == 0)
				throw new ContentKeyResolveImpossibleException("Cannot resolve content key as no usage rules are defined.");

			if (UsageRules.Any(r => r.ContainsUnsupportedFilters))
				throw new NotSupportedException("The usage rules in the CPIX document contain filters that are not supported by the current implementation. No content keys can be resolved.");

			var results = UsageRules
				.Where(rule => EvaluateUsageRule(rule, context))
				.Select(rule => ContentKeys.Single(key => key.Id == rule.KeyId))
				.ToArray();

			if (results.Length == 0)
				throw new ContentKeyResolveException("No content keys resolved to the content key context.");

			if (results.Length > 1)
				throw new ContentKeyResolveAmbiguityException("Multiple content keys resolved to the content key context.");

			return results[0];
		}

		/// <summary>
		/// Saves the CPIX document to a file, overwriting the existing contents.
		/// If any exception is thrown, the document may be left in an invalid state.
		/// </summary>
		public void Save(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			using (var file = File.Create(path))
				Save(file);
		}

		/// <summary>
		/// Saves the CPIX document to a stream. If any exception is thrown, the document may be left in an invalid state.
		/// </summary>
		public void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			// Validate all the entity collections.
			foreach (var collection in EntityCollections)
				collection.ValidateCollectionStateBeforeSave();

			ValidateConstraintsBetweenDrmSystemsAndContentKeys(DrmSystems, ContentKeys);

			// Saving is a multi - phase process:
			// 1) Create a new XML document, if needed. If we are working with a loaded document, we just use the existing one.
			//    Note that any removed items have already been removed from the XML document - at the moment of save
			//    it already contains the elements that will survive into the saved document (but not any new elements).
			// 2) Serialize any new delivery data for the recipients. Sign if needed.
			// 3) Serialize any new content keys. Sign if needed.
			// 4) Serialize any new usage rules. Sign if needed.
			// 5) Sign document if needed.
			// 6) Treat all items as having been newly loaded from the saved document (for forward consistency).
			// 7) Validate the document against schema to ensure that we did not accidentally generate invalid CPIX.
			// 8) Serialize the XML document to file (everything above happens in-memory).

			if (_xmlDocument == null)
			{
				_xmlDocument = CreateNewXmlDocument();
				_namespaceManager = XmlHelpers.CreateCpixNamespaceManager(_xmlDocument);
			}
			
			foreach (var collection in EntityCollections)
				collection.SaveChanges(_xmlDocument, _namespaceManager);

			// Save root attributes.
			var rootElement = _xmlDocument.DocumentElement;
			var contentIdAttribute = rootElement.GetAttributeNode(DocumentRootElement.ContentIdAttributeName);

			if (ContentId == null)
			{
				if (contentIdAttribute != null)
					rootElement.RemoveAttributeNode(contentIdAttribute);
			}
			else
			{
				if (contentIdAttribute == null)
					contentIdAttribute = rootElement.SetAttributeNode(DocumentRootElement.ContentIdAttributeName, null);

				contentIdAttribute.Value = ContentId;
			}

			// If a loaded signature has been removed and we have a new signer, sign the document!
			if (_documentSignature == null && _desiredSignedBy != null)
			{
				var signature = CryptographyHelpers.SignXmlElement(_xmlDocument, "", _desiredSignedBy);
				_documentSignature = new Tuple<XmlElement, X509Certificate2>(signature, _desiredSignedBy);
				_desiredSignedBy = null;
			}

			// We save to a temporary memory buffer for schema validation purposes, for platform API reasons.
			using (var buffer = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings
				{
					// NB! Do not apply any formatting here, as digital signatures have already been generated
					// and any formatting will invalidate the signatures!
					Encoding = new UTF8Encoding(false),
					CloseOutput = false
				}))
				{
					_xmlDocument.Save(writer);
				}

				// Reload the document from the buffer in order to validate it against schema (sanity check).
				buffer.Position = 0;
				LoadDocumentAndValidateAgainstSchema(buffer);

				// Success! Copy our buffer to the output stream.
				buffer.Position = 0;
				buffer.CopyTo(stream);
			}
		}

		private static void ValidateConstraintsBetweenDrmSystemsAndContentKeys(IEnumerable<DrmSystem> drmSystems, IEnumerable<ContentKey> contentKeys)
		{
			var existingKeys = contentKeys.Select(k => k.Id);
			var referencedKeys = drmSystems.Select(s => s.KeyId);

			if (referencedKeys.Except(existingKeys).Any())
				throw new InvalidCpixDataException("A content key must exist for all keys referenced by DRM system signaling entries.");
		}

		private static XmlDocument CreateNewXmlDocument()
		{
			var document = XmlHelpers.Serialize(new DocumentRootElement());
			document.Schemas = _schemaSet;

			// For documents we create from the start, let's include some useful namespace prefix declarations right
			// at the document root level, so we can reduce namespace spam in the document and improve human-readability.
			// We cannot do this for external documents for fear of conflicts but for our own blank documents, is no problem.
			XmlHelpers.DeclareNamespace(document.DocumentElement, "ds", Constants.XmlDigitalSignatureNamespace);
			XmlHelpers.DeclareNamespace(document.DocumentElement, "enc", Constants.XmlEncryptionNamespace);
			XmlHelpers.DeclareNamespace(document.DocumentElement, "pskc", Constants.PskcNamespace);

			// The signature generation code will still duplicate the namespace declaration because that's just what it
			// does but at least for the majority of "normal" content, this improves readability quite a lot.

			return document;
		}

		private static XmlDocument LoadDocumentAndValidateAgainstSchema(Stream stream)
		{
			var settings = new XmlReaderSettings
			{
				// NB! XML Schema validation is actually required for correct XML Digital Signature processing!
				// This is because without schema validation on load, newlines are mangled by .NET Framework
				// when executing the Canonical XML transformation. Therefore, always perform validation!
				// More info: https://connect.microsoft.com/VisualStudio/feedback/details/3002812/xmldsigc14ntransform-incorrectly-strips-whitespace-and-does-it-inconsistently
				ValidationType = ValidationType.Schema,

				// We are not the owner of the stream, so let's leave it open.
				CloseInput = false
			};

			settings.Schemas.Add(_schemaSet);

			var document = new XmlDocument();

			// NB! Whitespace in XML Digital Signature scope is signed, as well! Do not remove any whitespace!
			document.PreserveWhitespace = true;

			using (var reader = XmlReader.Create(stream, settings))
				document.Load(reader);

			return document;
		}

		/// <summary>
		/// Loads a CPIX document from a file, decrypting it using the key pairs
		/// of the identity referenced by the provided certificate, if required.
		/// </summary>
		/// <remarks>
		/// All digital signatures are verified. Note that a valid signature does not mean that the signer
		/// is trusted! It is the caller's responsibility to ensure that any signers are trusted!
		/// </remarks>
		public static CpixDocument Load(string path, X509Certificate2 recipientCertificate)
		{
			if (recipientCertificate == null)
				throw new ArgumentNullException(nameof(recipientCertificate));

			return Load(path, new[] { recipientCertificate });
		}

		/// <summary>
		/// Loads a CPIX document from a file, decrypting it using the key pairs
		/// of the identities referenced by the provided certificates, if required.
		/// </summary>
		/// <remarks>
		/// All digital signatures are verified. Note that a valid signature does not mean that the signer
		/// is trusted! It is the caller's responsibility to ensure that any signers are trusted!
		/// </remarks>
		public static CpixDocument Load(string path, IReadOnlyCollection<X509Certificate2> recipientCertificates = null)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			using (var file = File.OpenRead(path))
				return Load(file, recipientCertificates);
		}

		/// <summary>
		/// Loads a CPIX document from a stream, decrypting it using the key pairs
		/// of the identity referenced by the provided certificate, if required.
		/// </summary>
		/// <remarks>
		/// All digital signatures are verified. Note that a valid signature does not mean that the signer
		/// is trusted! It is the caller's responsibility to ensure that any signers are trusted!
		/// </remarks>
		public static CpixDocument Load(Stream stream, X509Certificate2 recipientCertificate)
		{
			if (recipientCertificate == null)
				throw new ArgumentNullException(nameof(recipientCertificate));

			return Load(stream, new[] { recipientCertificate });
		}

		/// <summary>
		/// Loads a CPIX document from a stream, decrypting it using the key pairs
		/// of the identities referenced by the provided certificates, if required.
		/// </summary>
		/// <remarks>
		/// All digital signatures are verified. Note that a valid signature does not mean that the signer
		/// is trusted! It is the caller's responsibility to ensure that any signers are trusted!
		/// </remarks>
		public static CpixDocument Load(Stream stream, IReadOnlyCollection<X509Certificate2> recipientCertificates = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (recipientCertificates != null)
				foreach (var certificate in recipientCertificates)
					CryptographyHelpers.ValidateRecipientCertificateAndPrivateKey(certificate);

			var xmlDocument = LoadDocumentAndValidateAgainstSchema(stream);

			if (xmlDocument.DocumentElement?.LocalName != "CPIX" || xmlDocument.DocumentElement?.NamespaceURI != Constants.CpixNamespace)
				throw new InvalidCpixDataException("The provided XML file does not appear to be a CPIX document - the name of the root element is incorrect.");

			// We will fill this instance with the loaded data.
			var document = new CpixDocument(xmlDocument, recipientCertificates);

			// Verify all signatures in the document.
			// If any signatures match one of the "known" scopes (a collection or the document), remember it.
			document.VerifyAllSignaturesAndRememberSigners();

			// Load the root element attributes.
			var documentRootElement = XmlHelpers.Deserialize<DocumentRootElement>(xmlDocument.DocumentElement);
			document._contentId = documentRootElement.ContentId;

			// Now load all the entity collections, doing the relevant logic at each step.
			foreach (var collection in document.EntityCollections)
				collection.Load(xmlDocument, document._namespaceManager);

			// And finally, do some basic sanity checking. We do this after load to ensure any cross-validation can be done.
			foreach (var collection in document.EntityCollections)
				collection.ValidateCollectionStateAfterLoad();

			ValidateConstraintsBetweenDrmSystemsAndContentKeys(document.DrmSystems, document.ContentKeys);

			// Sounds good to go!
			return document;
		}

		public CpixDocument()
		{
			Recipients = new RecipientCollection(this);
			ContentKeys = new ContentKeyCollection(this);
			DrmSystems = new DrmSystemCollection(this);
			ContentKeyPeriods = new ContentKeyPeriodCollection(this);
			UsageRules = new UsageRuleCollection(this);
		}

		#region Internal API
		/// <summary>
		/// Gets the collection of certificates referencing private keys that we can potentially use to receive CPIX data.
		/// </summary>
		internal IReadOnlyCollection<X509Certificate2> RecipientCertificates { get; private set; }

		/// <summary>
		/// Gets the document key or null if no document key has been loaded/generated
		/// </summary>
		internal byte[] DocumentKey { get; private set; }

		/// <summary>
		/// Gets the MAC key or null if no MAC key has been loaded/generated.
		/// </summary>
		internal byte[] MacKey { get; private set; }

		/// <summary>
		/// Generates a new document and MAC key.
		/// </summary>
		internal void GenerateKeys()
		{
			using (var random = RandomNumberGenerator.Create())
			{
				DocumentKey = new byte[Constants.DocumentKeyLengthInBytes];
				random.GetBytes(DocumentKey);

				MacKey = new byte[Constants.MacKeyLengthInBytes];
				random.GetBytes(MacKey);
			}
		}

		internal void ImportKeys(byte[] documentKey, byte[] macKey)
		{
			if (documentKey == null)
				throw new ArgumentNullException(nameof(documentKey));

			if (macKey == null)
				throw new ArgumentNullException(nameof(macKey));

			if (documentKey.Length != Constants.DocumentKeyLengthInBytes)
				throw new InvalidCpixDataException($"Invalid document key length. Expected {Constants.DocumentKeyLengthInBytes} bytes, received {documentKey.Length} bytes.");

			if (macKey.Length != Constants.MacKeyLengthInBytes)
				throw new InvalidCpixDataException($"Invalid MAC key length. Expected {Constants.MacKeyLengthInBytes} bytes, received {macKey.Length} bytes.");

			DocumentKey = documentKey;
			MacKey = macKey;
		}

		/// <summary>
		/// Throws an exception if the document is read-only.
		/// </summary>
		internal void VerifyIsNotReadOnly()
		{
			if (!IsReadOnly)
				return;

			throw new InvalidOperationException("The document is read-only. You must remove or re-apply any digital signatures on the document to make it writable.");
		}

		/// <summary>
		/// All the entity collections *in processing order*.
		/// </summary>
		internal IEnumerable<EntityCollectionBase> EntityCollections => new EntityCollectionBase[]
		{
			Recipients,
			ContentKeys,
			DrmSystems,
			ContentKeyPeriods,
			UsageRules
		};
		#endregion

		#region Implementation details
		private CpixDocument(XmlDocument loadedXml, IReadOnlyCollection<X509Certificate2> recipientCertificates) : this()
		{
			if (loadedXml == null)
				throw new ArgumentNullException(nameof(loadedXml));

			_xmlDocument = loadedXml;
			_namespaceManager = XmlHelpers.CreateCpixNamespaceManager(_xmlDocument);

			RecipientCertificates = recipientCertificates ?? new X509Certificate2[0];
		}

		// If this instance is not a new document, this references the XML structure it is based upon. The XML structure will
		// be modified live when anything is removed, though add operations are only serialized on Save().
		private XmlDocument _xmlDocument;

		// Namespace manager for the XML document in _xmlDocument;
		private XmlNamespaceManager _namespaceManager;

		// The actual signature that is actually present in the loaded document (if any).
		private Tuple<XmlElement, X509Certificate2> _documentSignature;

		// If _documentSignature is null, this is the desired identity whose signature will cover the document on save.
		// If _documentSignature is not null, this is the current signer of the document.
		private X509Certificate2 _desiredSignedBy;

		// Root-level attribute values.
		private string _contentId;

		private void VerifyAllSignaturesAndRememberSigners()
		{
			Func<EntityCollectionBase, string> tryGetCollectionSignatureReferenceUri = delegate (EntityCollectionBase collection)
			{
				var containerElement = (XmlElement)_xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:" + collection.ContainerName, _namespaceManager);

				if (containerElement == null)
					return null;

				var elementId = containerElement.GetAttribute("id");

				if (elementId == "")
					return null;

				return "#" + elementId;
			};

			// Maps reference URIs to entity collections for all collections that exist in XML and can be referenced.
			var collectionReferenceUris = EntityCollections
				.Select(c => new
				{
					Collection = c,
					ReferenceUri = tryGetCollectionSignatureReferenceUri(c)
				})
				.Where(x => x.ReferenceUri != null)
				.ToDictionary(x => x.ReferenceUri, x => x.Collection);

			foreach (XmlElement signature in _xmlDocument.SelectNodes("/cpix:CPIX/ds:Signature", _namespaceManager))
			{
				var signedXml = new SignedXml(_xmlDocument);
				signedXml.LoadXml(signature);

				// We verify all signatures using the data embedded within them.
				if (!signedXml.CheckSignature())
					throw new SecurityException("CPIX signature failed to verify - the document has been tampered with!");

				// The signature must include a certificate for the signer in order to be useful to us.
				var certificateElement = signature.SelectSingleNode("ds:KeyInfo/ds:X509Data/ds:X509Certificate", _namespaceManager);

				if (certificateElement == null)
					continue;

				var certificate = new X509Certificate2(Convert.FromBase64String(certificateElement.InnerText));

				var referenceUris = signedXml.SignedInfo.References.Cast<Reference>().Select(r => r.Uri).ToArray();

				if (referenceUris.Length != 1)
				{
					// Length is not 1? This is not any signature we recognize.
					continue;
				}

				var referenceUri = referenceUris.Single();

				if (referenceUri == "")
				{
					// This is a document-level signature.
					_documentSignature = new Tuple<XmlElement, X509Certificate2>(signature, certificate);
					_desiredSignedBy = certificate;
				}
				else if (collectionReferenceUris.ContainsKey(referenceUri))
				{
					// This is a signature on one of the entity collections.
					var collection = collectionReferenceUris[referenceUri];

					collection.ImportLoadedSignature(signature, certificate);
				}
				else
				{
					// Unknown thing was signed. We will just pretend the signature does not exist (besides verification).
				}
			}
		}

		private static bool EvaluateUsageRule(UsageRule rule, ContentKeyContext context)
		{
			// Each TYPE of filter is combined with AND.
			// Each filter of the SAME type is combined with OR.
			// OR is evaluated before AND.

			// We go through all the filter lists and return false if we find a filter list that all evaluate to false.

			if (rule.VideoFilters?.Count > 0 && !rule.VideoFilters.Any(f => EvaluateVideoFilter(f, context)))
				return false;

			if (rule.AudioFilters?.Count > 0 && !rule.AudioFilters.Any(f => EvaluateAudioFilter(f, context)))
				return false;

			if (rule.BitrateFilters?.Count > 0 && !rule.BitrateFilters.Any(f => EvaluateBitrateFilter(f, context)))
				return false;

			if (rule.LabelFilters?.Count > 0 && !rule.LabelFilters.Any(f => EvaluateLabelFilter(f, context)))
				return false;

			// All filter lists with anything in them said we are good to go. It's a match!
			return true;
		}

		private static bool EvaluateVideoFilter(VideoFilter filter, ContentKeyContext context)
		{
			if (context.Type != ContentKeyContextType.Video)
				return false;

			if (filter.MinPixels != null && !(context.PicturePixelCount >= filter.MinPixels))
				return false;
			if (filter.MaxPixels != null && !(context.PicturePixelCount <= filter.MaxPixels))
				return false;

			if (filter.MinFramesPerSecond != null && !(context.VideoFramesPerSecond > filter.MinFramesPerSecond))
				return false;
			if (filter.MaxFramesPerSecond != null && !(context.VideoFramesPerSecond <= filter.MaxFramesPerSecond))
				return false;

			if (filter.WideColorGamut != null && context.WideColorGamut != filter.WideColorGamut)
				return false;

			if (filter.HighDynamicRange != null && context.HighDynamicRange != filter.HighDynamicRange)
				return false;

			return true;
		}

		private static bool EvaluateAudioFilter(AudioFilter filter, ContentKeyContext context)
		{
			if (context.Type != ContentKeyContextType.Audio)
				return false;

			if (filter.MinChannels != null && !(context.AudioChannelCount >= filter.MinChannels))
				return false;
			if (filter.MaxChannels != null && !(context.AudioChannelCount <= filter.MaxChannels))
				return false;

			return true;
		}

		private static bool EvaluateBitrateFilter(BitrateFilter filter, ContentKeyContext context)
		{
			if (filter.MinBitrate != null && !(context.Bitrate >= filter.MinBitrate))
				return false;
			if (filter.MaxBitrate != null && !(context.Bitrate <= filter.MaxBitrate))
				return false;

			return true;
		}

		private static bool EvaluateLabelFilter(LabelFilter filter, ContentKeyContext context)
		{
			return context.Labels?.Any(l => l == filter.Label) == true;
		}

		static CpixDocument()
		{
			// Load the XSD for CPIX and all the referenced schemas.
			_schemaSet = LoadCpixSchema();
		}

		private static XmlSchemaSet LoadCpixSchema()
		{
			var schemaSet = new XmlSchemaSet();

			var assembly = Assembly.GetExecutingAssembly();

			// At least one of the schemas contains DTDs, so let's say it's okay to process them.
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse
			};

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.xenc-schema.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.xmldsig-core-schema.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.pskc.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				schemaSet.Add(null, reader);

			using (var stream = assembly.GetManifestResourceStream("Axinom.Cpix.cpix.xsd"))
			using (var reader = XmlReader.Create(stream, settings))
				schemaSet.Add(null, reader);

			schemaSet.Compile();

			return schemaSet;
		}

		// Contains all the XML Schema information required to validate a CPIX document.
		private static readonly XmlSchemaSet _schemaSet;
		#endregion
	}
}
