using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Axinom.Cpix
{
	/// <summary>
	/// Base class for entity collections. This contains all the functionality that does not depend
	/// on the specific type of entity contained in the collection but is valid for all collections.
	/// </summary>
	public abstract class EntityCollectionBase
	{
		/// <summary>
		/// Gets the count of items in the collection.
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Removes all items from the collection.
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Gets whether the collection is read-only. Always returns true if the entire document is read-only.
		/// 
		/// The collection is read-only if you are dealing with a loaded CPIX document that contains signatures covering this
		/// collection. Remove any collection-scoped signatures and document-scoped signatures to make the collection writable.
		/// </summary>
		public bool IsReadOnly => Document.IsReadOnly || _loadedSignatures.Any();

		/// <summary>
		/// Applies a digital signature to the collection.
		/// 
		/// The signature is generated when the document is saved, so you can still modify the collection after this call.
		/// </summary>
		public void AddSignature(X509Certificate2 signerCertificate)
		{
			if (signerCertificate == null)
				throw new ArgumentNullException(nameof(signerCertificate));

			// Cannot add signatures to the collection if the document itself is signed!
			Document.VerifyIsNotReadOnly();

			if (SignedBy.Contains(signerCertificate))
				throw new InvalidOperationException("The collection is already signed by this identity.");

			CryptographyHelpers.ValidateSignerCertificate(signerCertificate);

			_newSigners.Add(signerCertificate);
		}

		/// <summary>
		/// Gets the certificates of the identities that have signed this collection.
		/// </summary>
		public IEnumerable<X509Certificate2> SignedBy => LoadedSigners.Concat(_newSigners);

		/// <summary>
		/// Removes all digital signatures that apply to this collection.
		/// </summary>
		public void RemoveAllSignatures()
		{
			foreach (var signature in _loadedSignatures)
				signature.Item1.ParentNode.RemoveChild(signature.Item1);

			_loadedSignatures.Clear();
			_newSigners.Clear();
		}

		#region Internal API
		/// <summary>
		/// Undecorated name of the XML element that serves as the container for this collection.
		/// </summary>
		internal abstract string ContainerName { get; }

		/// <summary>
		/// Saves any changes in this entity set to the supplied document.
		/// </summary>
		internal abstract void SaveChanges(XmlDocument document, XmlNamespaceManager namespaces);

		/// <summary>
		/// Loads the entity set from the supplied XML document.
		/// </summary>
		internal abstract void Load(XmlDocument document, XmlNamespaceManager namespaces);

		internal void ImportLoadedSignature(XmlElement signature, X509Certificate2 certificate)
		{
			_loadedSignatures.Add(new Tuple<XmlElement, X509Certificate2>(signature, certificate));
		}

		/// <summary>
		/// Performs validation of the collection and its contents before it is saved.
		/// </summary>
		internal virtual void ValidateCollectionStateBeforeSave()
		{
			ValidateEntitiesBeforeSave();
		}

		/// <summary>
		/// Performs validation of the collection and its contents after it has been loaded.
		/// </summary>
		internal virtual void ValidateCollectionStateAfterLoad()
		{
			ValidateEntitiesAfterLoad();
		}
		#endregion

		#region Protected API
		protected CpixDocument Document { get; }

		protected abstract IEnumerable<Entity> LoadedEntities { get; }
		protected abstract IEnumerable<Entity> NewEntities { get; }

		protected IEnumerable<X509Certificate2> NewSigners => _newSigners;
		protected IEnumerable<X509Certificate2> LoadedSigners => _loadedSignatures.Select(s => s.Item2);

		protected EntityCollectionBase(CpixDocument document)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			Document = document;
		}

		/// <summary>
		/// Throws an exception if the collection is read-only.
		/// 
		/// You should first verify that the document is not read-only, to provide most specific user feedback.
		/// </summary>
		protected void VerifyNotReadOnly()
		{
			if (!IsReadOnly)
				return;

			throw new InvalidOperationException("The entity collection is read-only. You must remove or re-apply any digital signatures on the collection to make it writable.");
		}

		protected void SaveNewSignatures(XmlDocument document, XmlElement containerElement)
		{
			if (!NewSigners.Any())
				return;

			// We need an ID on the element to sign it, so let's give it the same ID as its name.
			// Unless it already has an ID, of course, in which case use the existing one.
			var elementId = containerElement.GetAttribute("id");

			if (elementId == "")
			{
				containerElement.SetAttribute("id", ContainerName);
				elementId = ContainerName;
			}

			// Add any signatures and mark them as applied.
			foreach (var signer in _newSigners.ToArray())
			{
				var signature = CryptographyHelpers.SignXmlElement(document, elementId, signer);

				_newSigners.Remove(signer);
				_loadedSignatures.Add(new Tuple<XmlElement, X509Certificate2>(signature, signer));
			}
		}
		#endregion

		#region Implementation details
		private readonly List<X509Certificate2> _newSigners = new List<X509Certificate2>();
		private readonly List<Tuple<XmlElement, X509Certificate2>> _loadedSignatures = new List<Tuple<XmlElement, X509Certificate2>>();

		/// <summary>
		/// Performs validation of the collection's contents before it is saved.
		/// </summary>
		private void ValidateEntitiesBeforeSave()
		{
			// Just individually validate each new item.
			foreach (var item in NewEntities)
				item.ValidateNewEntity(Document);

			// No need to validate any loaded entities, as we won't save them even if modified.
		}

		/// <summary>
		/// Performs validation of the collection's contents after it has been loaded.
		/// </summary>
		private void ValidateEntitiesAfterLoad()
		{
			foreach (var item in LoadedEntities)
				item.ValidateLoadedEntity(Document);
		}
		#endregion
	}
}
