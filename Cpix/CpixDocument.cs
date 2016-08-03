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

namespace Axinom.Cpix
{
	/// <summary>
	/// A CPIX document, either created anew for saving or loaded from file for reading and/or modifying.
	/// </summary>
	/// <remarks>
	/// All content keys are automatically encrypted on save and delivery data generated for all recipients,
	/// unless no recipients are defined, in which case the content keys are saved in the clear.
	/// 
	/// There are two scopes of digital signatures supported by this implementation:
	/// * Any number of signatures may be applied to each entity collection.
	/// * Oee signature may be applied to the entire document.
	/// 
	/// Any signatures whose scopes do not match the above data sets are validated on load but otherwise ignored,
	/// though validated again on save to ensure that files that fail verification are inadvertently not created.
	/// 
	/// You must have the private keys to re-sign any of these parts that you modify,
	/// removing any existing signature before performing your modifications.
	/// 
	/// You must have a delivery key in order to modify a loaded set of content keys.
	/// </remarks>
	public sealed class CpixDocument
	{
		/// <summary>
		/// Gets the set of recipients that the CPIX document is meant to be securely delivered to.
		/// 
		/// If this collection is empty, the content keys within the CPIX document are not encrypted.
		/// </summary>
		public EntityCollection<IRecipient, Recipient> Recipients { get; }

		/// <summary>
		/// Gets the set of content keys stored in the CPIX document.
		/// </summary>
		public EntityCollection<IContentKey, ContentKey> ContentKeys { get; }

		/// <summary>
		/// Gets the set of usage rules stored in the CPIX document.
		/// </summary>
		public EntityCollection<IUsageRule, UsageRule> UsageRules { get; }

		/// <summary>
		/// Whether the values of content keys are readable.
		/// If false, you can only read the metadata, not the values themselves.
		/// 
		/// This is always true for new documents. This may be false for loaded documents
		/// if the content keys are encrypted and we do not possess any of the delivery keys.
		/// </summary>
		public bool ContentKeysAreReadable => Recipients.ExistingItems.Count() == 0 || DocumentKey != null;

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
		/// Set to null to remove any existing signature from the document.
		/// </summary>
		public X509Certificate2 SignedBy
		{
			get { return _desiredSignedBy; }
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
		/// Finds a suitable content key to samples that conform to the provided sample description.
		/// The content key assignment rules are resolved and exactly one match is assumed. No match
		/// or multiple matches are considered an error condition (ambiguities are not allowed).
		/// </summary>
		public IContentKey ResolveContentKey(SampleDescription sampleDescription)
		{
			if (sampleDescription == null)
				throw new ArgumentNullException(nameof(sampleDescription));

			sampleDescription.Validate();

			throw new NotImplementedException();

			//var results = UsageRules
			//	.Where(rule => EvaluateUsageRule(rule, sampleDescription))
			//	.Select(rule => ContentKeys.Single(key => key.Id == rule.KeyId))
			//	.ToArray();

			//if (results.Length == 0)
			//	throw new ContentKeyResolveException("No content keys were assigned to samples matching the provided sample description.");

			//if (results.Length > 1)
			//	throw new ContentKeyResolveException("Multiple content keys were assigned to samples matching the provided sample description.");

			//return results[0];
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

			// Saving is a multi - phase process:
			// 1) Create a new XML document, if needed. If we are working with a loaded document, we just use the existing one.
			//    Note that any removed items have already been removed from the XML document - at the moment of save
			//    it already contains the elements that will survive into the saved document (but not any new elements).
			// 2) Serialize any new delivery data for the recipients. Sign if needed.
			// 3) Serialize any new content keys. Sign if needed.
			// 4) Serialize any new usage rules. Sign if needed.
			// 5) Sign document if needed.
			// 6) Treat all items as having been newly loaded from the saved document (for forward consistency).
			// 7) Validate the document against schema to ensure that we did not accidetally generate invalid CPIX.
			// 8) Serialize the XML document to file (everything above happens in-memory).

			if (_xmlDocument == null)
			{
				_xmlDocument = CreateNewXmlDocument();
				_namespaceManager = CreateNamespaceManager(_xmlDocument);
			}

			foreach (var collection in EntityCollections)
				collection.SaveChanges(_xmlDocument, _namespaceManager);

			// Sign the document!
			if (_desiredSignedBy != null)
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
					Encoding = Encoding.UTF8
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

		private static XmlDocument CreateNewXmlDocument()
		{
			var document = XmlHelpers.XmlObjectToXmlDocument(new DocumentRootElement());
			document.Schemas = _schemaSet;

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

			if (xmlDocument.DocumentElement?.Name != "CPIX" || xmlDocument.DocumentElement?.NamespaceURI != Constants.CpixNamespace)
				throw new InvalidCpixDataException("The provided XML file does not appear to be a CPIX document - the name of the root element is incorrect.");

			// We will fill this instance with the loaded data.
			var document = new CpixDocument(xmlDocument, recipientCertificates);

			// Verify all signatures in the document.
			// If any signatures match one of the "known" scopes (a collection or the document), remember it.
			document.VerifyAllSignaturesAndRememberSigners();

			// Now load all the entity collections, doing the relevant logic at each step.
			foreach (var collection in document.EntityCollections)
				collection.Load(xmlDocument, document._namespaceManager);

			// And finally, do some basic sanity checking. We do this after load to ensure any cross-validation can be done.
			foreach (var collection in document.EntityCollections)
				collection.ValidateCollectionStateAfterLoad();

			// Sounds good to go!
			return document;
		}

		public CpixDocument()
		{
			Recipients = new RecipientCollection(this);
			ContentKeys = new ContentKeyCollection(this);
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
			DocumentKey = new byte[Constants.DocumentKeyLengthInBytes];
			Random.GetBytes(DocumentKey);

			MacKey = new byte[Constants.MacKeyLengthInBytes];
			Random.GetBytes(MacKey);
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
		/// Gets a random number generator associated with this instance of the document.
		/// </summary>
		internal readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
		#endregion

		#region Implementation details
		/// <summary>
		/// All the entity collections *in processing order*.
		/// </summary>
		private IEnumerable<EntityCollectionBase> EntityCollections => new EntityCollectionBase[]
		{
			Recipients,
			ContentKeys,
			UsageRules
		};

		private CpixDocument(XmlDocument loadedXml, IReadOnlyCollection<X509Certificate2> recipientCertificates) : this()
		{
			if (loadedXml == null)
				throw new ArgumentNullException(nameof(loadedXml));

			_xmlDocument = loadedXml;
			_namespaceManager = CreateNamespaceManager(_xmlDocument);

			RecipientCertificates = recipientCertificates ?? new X509Certificate2[0];
		}

		// If this instance is not a new document, this references the XML structure it is based upon. The XML structure will
		// be modified live when anything is removed, though add operations are only serialized on Save().
		private XmlDocument _xmlDocument;

		// Namespace manager for the XML document in _xmlDocument;
		private XmlNamespaceManager _namespaceManager;

		// The actual signature that is actually present in the loaded document (if any).
		private Tuple<XmlElement, X509Certificate2> _documentSignature;

		// The desired identity whose signature should cover the document.
		private X509Certificate2 _desiredSignedBy;

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

					collection.ImportExistingSignature(signature, certificate);
				}
				else
				{
					// Unknown thing was signed. We will just pretend the signature does not exist (besides verification).
				}
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
