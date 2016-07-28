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
	/// </summary>
	public sealed class EntityCollection<TEntity> : ICollection<TEntity> where TEntity : Entity
	{
		public void Add(TEntity item)
		{
			VerifyNotReadOnly();

			if (Contains(item))
				return; // Weird but whatever.

			// TODO: Maybe do initial sanity checking of the entity here?
			// It can still be modified later but at least as a first-step error check, it might be useful.

			_addedItems.Add(item);
		}

		public void Clear()
		{
			VerifyNotReadOnly();

			_addedItems.Clear();
			_loadedItems.Clear();
		}

		public bool Contains(TEntity item) => Items.Contains(item);

		public void CopyTo(TEntity[] array, int arrayIndex)
		{
			var items = Items.ToArray();
			Array.Copy(items, 0, array, arrayIndex, items.Length);
		}

		public bool Remove(TEntity item)
		{
			VerifyNotReadOnly();

			return _addedItems.Remove(item) || _loadedItems.Remove(item);
		}

		public IEnumerator<TEntity> GetEnumerator() => Items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

		/// <summary>
		/// Gets whether the collection is read-only. Always returns true if the entire document is read-only.
		/// 
		/// The collection is read-only if you are dealing with a loaded CPIX document that contains signatures covering this
		/// collection. Remove any collection-scoped signatures and document-scoped signatures to make the collection writable.
		/// </summary>
		public bool IsReadOnly => _document.IsReadOnly || _loadedSignatures.Any();

		/// <summary>
		/// Applies a digital signature to the collection.
		/// 
		/// The signature is generated when the document is saved, so you can still modify the collection after this call.
		/// </summary>
		public void AddSignature(X509Certificate2 signer)
		{
			_document.VerifyIsNotReadOnly();

			if (SignedBy.Contains(signer))
				return; // Okaaay, whatever.

			// TODO: Verify that certificate and key pair is sensible.

			_addedSigners.Add(signer);
		}

		/// <summary>
		/// Gets the certificates of the identities that have signed this collection.
		/// </summary>
		public IEnumerable<X509Certificate2> SignedBy => LoadedSigners.Concat(_addedSigners);

		/// <summary>
		/// Removes all digital signatures that apply to this collection.
		/// </summary>
		public void RemoveAllSignatures()
		{
			foreach (var signature in _loadedSignatures)
				signature.Item1.ParentNode.RemoveChild(signature.Item1);

			_loadedSignatures.Clear();
			_addedSigners.Clear();
		}

		/// <summary>
		/// Gets the number of items in the list.
		/// </summary>
		public int Count => Items.Count();

		internal EntityCollection(Document document)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			_document = document;
		}

		private readonly Document _document;

		private readonly List<TEntity> _addedItems = new List<TEntity>();
		private readonly List<TEntity> _loadedItems = new List<TEntity>();

		// All entities that exist in the list.
		private IEnumerable<TEntity> Items => _loadedItems.Concat(_addedItems);

		private readonly List<X509Certificate2> _addedSigners = new List<X509Certificate2>();
		private readonly List<Tuple<XmlElement, X509Certificate2>> _loadedSignatures = new List<Tuple<XmlElement, X509Certificate2>>();

		private IEnumerable<X509Certificate2> LoadedSigners => _loadedSignatures.Select(s => s.Item2);

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
	}
}