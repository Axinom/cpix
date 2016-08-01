using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Axinom.Cpix
{
	/// <summary>
	/// A collection of entities stored in a CPIX document.
	/// 
	/// An item in the collection may be freely modified after adding, before the document is first saved but
	/// items loaded from an existing document (and any saved entities) are read-only and must be replaced in whole.
	/// </summary>
	/// <remarks>
	/// The entity interface must be immutable but the implementation should be mutable. Adding new entities into the
	/// collection takes place by creating an instance of the implementation class and adding it to the collection,
	/// after which it is treated as immutable.
	/// 
	/// This may not always be enforced by the API for technical/convenience reasons but is always the principle
	/// to follow. Violations may lead to undefined behavior.
	/// </remarks>
	public abstract class EntityCollection<TEntityInterface, TEntityImplementation> : ICollection<TEntityInterface> where TEntityImplementation : Entity, TEntityInterface
	{
		/// <summary>
		/// Adds a new item to the collection.
		/// The item will be validated and should not be modified by the caller after this.
		/// </summary>
		public void Add(TEntityInterface itemAsInterface)
		{
			if (itemAsInterface == null)
				throw new ArgumentNullException(nameof(itemAsInterface));

			var item = itemAsInterface as TEntityImplementation;

			if (item == null)
				throw new ArgumentException("Entities added into this collection must be of type " + typeof(TEntityImplementation).Name);

			VerifyNotReadOnly();

			if (Contains(item))
				throw new InvalidOperationException("The item is already in this collection.");

			item.ValidateNewEntity();

			ValidateCollectionStateBeforeAdd(item);

			_newItems.Add(item);
		}

		public void Clear()
		{
			VerifyNotReadOnly();

			_newItems.Clear();

			// We also delete any XML elements for loaded items.
			foreach (var data in _existingItemsData)
				data.Item2.ParentNode.RemoveChild(data.Item2);

			_existingItemsData.Clear();
		}

		public bool Contains(TEntityInterface item) => AllItems.Contains(item);

		public void CopyTo(TEntityInterface[] array, int arrayIndex)
		{
			var items = AllItems.ToArray();
			Array.Copy(items, 0, array, arrayIndex, items.Length);
		}

		public bool Remove(TEntityInterface itemAsInterface)
		{
			VerifyNotReadOnly();

			var item = itemAsInterface as TEntityImplementation;

			if (item == null)
				return false;

			if (_newItems.Remove(item))
				return true;

			var loadedItemData = _existingItemsData.SingleOrDefault(d => d.Item1 == item);

			if (loadedItemData == null)
				return false;

			loadedItemData.Item2.ParentNode.RemoveChild(loadedItemData.Item2);
			_existingItemsData.Remove(loadedItemData);
			return true;
		}

		public IEnumerator<TEntityInterface> GetEnumerator() => AllItems.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => AllItems.GetEnumerator();

		/// <summary>
		/// Gets whether the collection is read-only. Always returns true if the entire document is read-only.
		/// 
		/// The collection is read-only if you are dealing with a loaded CPIX document that contains signatures covering this
		/// collection. Remove any collection-scoped signatures and document-scoped signatures to make the collection writable.
		/// </summary>
		public bool IsReadOnly => _document.IsReadOnly || _existingSignatures.Any();

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
			_document.VerifyIsNotReadOnly();

			if (SignedBy.Contains(signerCertificate))
				throw new InvalidOperationException("The collection is already signed by this identity.");

			CryptographyHelpers.ValidateSignerCertificate(signerCertificate);

			_newSigners.Add(signerCertificate);
		}

		/// <summary>
		/// Gets the certificates of the identities that have signed this collection.
		/// </summary>
		public IEnumerable<X509Certificate2> SignedBy => ExistingSigners.Concat(_newSigners);

		/// <summary>
		/// Removes all digital signatures that apply to this collection.
		/// </summary>
		public void RemoveAllSignatures()
		{
			foreach (var signature in _existingSignatures)
				signature.Item1.ParentNode.RemoveChild(signature.Item1);

			_existingSignatures.Clear();
			_newSigners.Clear();
		}

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count => AllItems.Count();

		#region Implementation details
		internal EntityCollection(CpixDocument document)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			_document = document;
		}

		protected readonly CpixDocument _document;

		protected readonly List<TEntityImplementation> _newItems = new List<TEntityImplementation>();
		private readonly List<Tuple<TEntityImplementation, XmlElement>> _existingItemsData = new List<Tuple<TEntityImplementation, XmlElement>>();

		internal IEnumerable<TEntityImplementation> ExistingItems => _existingItemsData.Select(data => data.Item1);

		internal IEnumerable<TEntityImplementation> AllItems => ExistingItems.Concat(_newItems);

		private readonly List<X509Certificate2> _newSigners = new List<X509Certificate2>();
		private readonly List<Tuple<XmlElement, X509Certificate2>> _existingSignatures = new List<Tuple<XmlElement, X509Certificate2>>();

		private IEnumerable<X509Certificate2> ExistingSigners => _existingSignatures.Select(s => s.Item2);

		/// <summary>
		/// Throws an exception if the collection is read-only.
		/// You should first verify that the document is not read-only.
		/// </summary>
		private void VerifyNotReadOnly()
		{
			if (!IsReadOnly)
				return;

			throw new InvalidOperationException("The entity collection is read-only. You must remove or re-apply any digital signatures on the collection to make it writable.");
		}

		/// <summary>
		/// Performs collection-scope validation before an entity is added to the collection.
		/// The entity has already passed individual validation, so this just concerns "global" state validation.
		/// </summary>
		protected virtual void ValidateCollectionStateBeforeAdd(TEntityImplementation entity)
		{
		}

		/// <summary>
		/// Performs validation of the collection and its contents before it is saved.
		/// </summary>
		internal virtual void ValidateCollectionStateBeforeSave()
		{
			ValidateEntitiesBeforeSave();
		}

		/// <summary>
		/// Performs validation of the collection's contents before it is saved.
		/// </summary>
		internal void ValidateEntitiesBeforeSave()
		{
			// Just individually validate each new item.
			foreach (var item in _newItems)
				item.ValidateNewEntity();

			// No need to validate any existing items, as we won't save them even if modified.
		}

		/// <summary>
		/// Saves any changes in this entity set to the supplied document.
		/// </summary>
		internal void SaveChanges(XmlDocument document, XmlNamespaceManager namespaces)
		{
			if (IsReadOnly)
				return;

			var containerElement = (XmlElement)document.SelectSingleNode("/cpix:CPIX/cpix:" + ContainerName, namespaces);

			if (Count == 0 && SignedBy.Count() == 0)
			{
				// We don't have any contents to put in it AND we don't have any signatures to apply.
				// This is the only scenario where we do not need the container element in the document, so remove it
				// and consider the save operation completed. All other paths follow the longer logic chain.

				if (containerElement != null)
					containerElement.ParentNode.RemoveChild(containerElement);

				return;
			}

			// We need a container element, so create one if it is missing.
			if (containerElement == null)
				containerElement = (XmlElement)document.DocumentElement.AppendChild(document.CreateElement("cpix:" + ContainerName, Constants.CpixNamespace));

			// Add any new items and then mark them as existing items.
			foreach (var item in _newItems.ToArray())
			{
				var element = SerializeEntity(document, namespaces, containerElement, item);

				_newItems.Remove(item);
				_existingItemsData.Add(new Tuple<TEntityImplementation, XmlElement>(item, element));
			}

			// Add any signatures and mark them as applied.
			if (_newSigners.Any())
			{
				// We need an ID on the element to sign it, so let's give it the same ID as its name.
				containerElement.SetAttribute("id", ContainerName);

				foreach (var signer in _newSigners.ToArray())
				{
					var signature = CryptographyHelpers.SignXmlElement(document, ContainerName, signer);

					_newSigners.Remove(signer);
					_existingSignatures.Add(new Tuple<XmlElement, X509Certificate2>(signature, signer));
				}
			}
		}

		/// <summary>
		/// Serializes an entity into the indicated container in the XML document, returning the newly created XML element.
		/// </summary>
		protected abstract XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, TEntityImplementation entity);

		/// <summary>
		/// Undecorated name of the XML element that serves as the container for this collection.
		/// </summary>
		protected abstract string ContainerName { get; }
		#endregion
	}
}