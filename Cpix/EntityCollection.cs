using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public abstract class EntityCollection<TEntityInterface, TEntityImplementation> : EntityCollectionBase, ICollection<TEntityInterface> where TEntityImplementation : Entity, TEntityInterface
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
				throw new ArgumentException("The item is already in this collection.");

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
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count => AllItems.Count();

		#region Internal API
		internal IEnumerable<TEntityImplementation> NewItems => _newItems;
		internal IEnumerable<TEntityImplementation> ExistingItems => _existingItemsData.Select(data => data.Item1);
		internal IEnumerable<TEntityImplementation> AllItems => ExistingItems.Concat(_newItems);

		protected override IEnumerable<Entity> ExistingEntities => ExistingItems;
		protected override IEnumerable<Entity> NewEntities => _newItems;

		internal override void SaveChanges(XmlDocument document, XmlNamespaceManager namespaces)
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
			{
				var element = document.CreateElement("cpix:" + ContainerName, Constants.CpixNamespace);
				containerElement = XmlHelpers.InsertTopLevelCpixXmlElementInCorrectOrder(element, document);
			}

			// Add any new items and then mark them as existing items.
			foreach (var item in _newItems.ToArray())
			{
				var element = SerializeEntity(document, namespaces, containerElement, item);

				_newItems.Remove(item);
				_existingItemsData.Add(new Tuple<TEntityImplementation, XmlElement>(item, element));
			}

			SaveNewSignatures(document, containerElement);
		}

		internal override void Load(XmlDocument document, XmlNamespaceManager namespaces)
		{
			var containerElement = document.SelectSingleNode("/cpix:CPIX/cpix:" + ContainerName, namespaces);

			if (containerElement == null)
				return; // No data.

			// We assume that each child element is an item in the collection (enforced by schema).
			foreach (XmlElement element in containerElement.ChildNodes.Cast<XmlNode>().Where(node => node.NodeType == XmlNodeType.Element))
			{
				// Entities will all be validated later, when everything is loaded (to simplify reference handling).
				var entity = DeserializeEntity(element, namespaces);
				_existingItemsData.Add(new Tuple<TEntityImplementation, XmlElement>(entity, element));
			}
		}
		#endregion

		#region Protected API
		protected EntityCollection(CpixDocument document) : base(document)
		{
		}

		/// <summary>
		/// Serializes an entity into the indicated container in the XML document, returning the newly created XML element.
		/// </summary>
		protected abstract XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, TEntityImplementation entity);

		/// <summary>
		/// Deserializes an entity from an XML document, returning it.
		/// </summary>
		protected abstract TEntityImplementation DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces);

		/// <summary>
		/// Performs collection-scope validation before an entity is added to the collection.
		/// The entity has already passed individual validation, so this just concerns "global" state validation.
		/// </summary>
		protected virtual void ValidateCollectionStateBeforeAdd(TEntityImplementation entity)
		{
		}
		#endregion

		#region Implementation details
		private readonly List<TEntityImplementation> _newItems = new List<TEntityImplementation>();
		private readonly List<Tuple<TEntityImplementation, XmlElement>> _existingItemsData = new List<Tuple<TEntityImplementation, XmlElement>>();
		#endregion
	}
}