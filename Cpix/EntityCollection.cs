using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Axinom.Cpix
{
	/// <summary>
	/// A collection of entities stored in a CPIX document.
	/// </summary>
	/// <remarks>
	/// An item added to the collection must not be modified after Add().
	/// An item retrieved from the collection must not be modified, ever.
	/// 
	/// Violation of these constraints may lead to undefined behavior.
	/// </remarks>
	public abstract class EntityCollection<TEntity> : EntityCollectionBase, ICollection<TEntity> where TEntity : Entity
	{
		/// <summary>
		/// Adds a new item to the collection.
		/// The item will be validated and should not be modified by the caller after this.
		/// </summary>
		public void Add(TEntity item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			VerifyNotReadOnly();

			if (Contains(item))
				throw new ArgumentException("The item is already in this collection.");

			item.ValidateNewEntity(Document);

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

		public bool Contains(TEntity item) => AllItems.Contains(item);

		public void CopyTo(TEntity[] array, int arrayIndex)
		{
			var items = AllItems.ToArray();
			Array.Copy(items, 0, array, arrayIndex, items.Length);
		}

		public bool Remove(TEntity item)
		{
			VerifyNotReadOnly();

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

		public IEnumerator<TEntity> GetEnumerator() => AllItems.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => AllItems.GetEnumerator();

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public override int Count => AllItems.Count();

		#region Internal API
		internal IEnumerable<TEntity> NewItems => _newItems;
		internal IEnumerable<TEntity> ExistingItems => _existingItemsData.Select(data => data.Item1);
		internal IEnumerable<TEntity> AllItems => ExistingItems.Concat(_newItems);

		protected override IEnumerable<Entity> ExistingEntities => ExistingItems;
		protected override IEnumerable<Entity> NewEntities => _newItems;

		internal override void SaveChanges(XmlDocument document, XmlNamespaceManager namespaces)
		{
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
				_existingItemsData.Add(new Tuple<TEntity, XmlElement>(item, element));
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
				_existingItemsData.Add(new Tuple<TEntity, XmlElement>(entity, element));
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
		protected abstract XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, TEntity entity);

		/// <summary>
		/// Deserializes an entity from an XML document, returning it.
		/// </summary>
		protected abstract TEntity DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces);

		/// <summary>
		/// Performs collection-scope validation before an entity is added to the collection.
		/// The entity has already passed individual validation, so this just concerns "global" state validation.
		/// </summary>
		protected virtual void ValidateCollectionStateBeforeAdd(TEntity entity)
		{
		}
		#endregion

		#region Implementation details
		private readonly List<TEntity> _newItems = new List<TEntity>();
		private readonly List<Tuple<TEntity, XmlElement>> _existingItemsData = new List<Tuple<TEntity, XmlElement>>();
		#endregion
	}
}