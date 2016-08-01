using Axinom.Cpix.Compatibility;
using Axinom.Cpix.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
		public bool ContentKeysAreReadable { get; private set; } = true;

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
			Recipients.ValidateCollectionStateBeforeSave();
			ContentKeys.ValidateCollectionStateBeforeSave();
			UsageRules.ValidateCollectionStateBeforeSave();

			// Saving is a multi - phase process:
			// 1) Create a new XML document, if needed. If we are working with a loaded document, we just use the existing one.
			//    Note that any removed items have already been removed from the XML document - at the moment of save
			//    it already contains the elements that will survive into the saved document (but not any new elements).
			// 2) Serialize any new delivery data for the recipients. Sign if needed.
			// 3) Serialize any new content keys. Sign if needed.
			// 4) Serialize any new usazge rules. Sign if needed.
			// 5) Sign document if needed.
			// 6) Treat all items as having been newly loaded from the saved document (for forward consistency).
			// 7) Validate the document against schema to ensure that we did not accidetally generate invalid CPIX.
			// 8) Serialize the XML document to file (everything above happens in-memory).

			XmlDocument document = _loadedXml ?? CreateNewXmlDocument();
			XmlNamespaceManager namespaces = CreateNamespaceManager(document);

			Recipients.SaveChanges(document, namespaces);
			ContentKeys.SaveChanges(document, namespaces);
			UsageRules.SaveChanges(document, namespaces);

			// Sign the document!
			if (_desiredSignedBy != null)
			{
				var signature = CryptographyHelpers.SignXmlElement(document, "", _desiredSignedBy);
				_documentSignature = new Tuple<XmlElement, X509Certificate2>(signature, _desiredSignedBy);
				_desiredSignedBy = null;
			}

			// We save to a temporary memory buffer for schema validation purposes, for platform API reasons.
			using (var buffer = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings
				{
					// The serialization we do results in some duplicate namespace declarations being generated.
					// Hmm... does this correctly handle already signed data? Let's hope so!
					NamespaceHandling = NamespaceHandling.OmitDuplicates,
					Indent = true,
					IndentChars = "\t",
					Encoding = Encoding.UTF8
				}))
				{
					document.Save(writer);
				}

				buffer.Position = 0;

				// To validate the output, we read it into a new temporary XmlDocument.
				var settings = new XmlReaderSettings
				{
					ValidationType = ValidationType.Schema
				};
				settings.Schemas.Add(_schemaSet);

				var validationDocument = new XmlDocument();

				// This will throw if there are schema errors.
				validationDocument.Load(XmlReader.Create(buffer, settings));

				// Success! Copy our buffer to the output stream.
				buffer.Position = 0;
				buffer.CopyTo(stream);
			}
		}

		private XmlDocument CreateNewXmlDocument()
		{
			return XmlHelpers.XmlObjectToXmlDocument(new DocumentRootElement());
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
			throw new NotImplementedException();
		}

		public CpixDocument()
		{
			Recipients = new RecipientCollection(this);
			ContentKeys = new ContentKeyCollection(this);
			UsageRules = new UsageRuleCollection(this);
		}

		#region Implementation details
		private CpixDocument(XmlDocument loadedXml) : this()
		{
			if (loadedXml == null)
				throw new ArgumentNullException(nameof(loadedXml));

			_loadedXml = loadedXml;
		}

		// If this instance is not a new document, this references the XML structure it is based upon. The XML structure will
		// be modified live when anything is removed, though add operations are only serialized on Save().
		private XmlDocument _loadedXml;

		// The actual signature that is actually present in the loaded document (if any).
		private Tuple<XmlElement, X509Certificate2> _documentSignature;

		// The desired identity whose signature should cover the document.
		private X509Certificate2 _desiredSignedBy;

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

		/// <summary>
		/// Throws an exception if the document is read-only.
		/// </summary>
		internal void VerifyIsNotReadOnly()
		{
			if (!IsReadOnly)
				return;

			throw new InvalidOperationException("The document is read-only. You must remove or re-apply any digital signatures on the document to make it writable.");
		}

		internal static XmlNamespaceManager CreateNamespaceManager(XmlDocument document)
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
			// For compatibility with .NET Framework < 4.6.2.
			// Remove once 4.6.2 is published and can be safely targeted.
			RSAPKCS1SHA512SignatureDescription.Register();

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

		internal readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
		#endregion
	}
}
