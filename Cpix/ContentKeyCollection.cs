using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class ContentKeyCollection : EntityCollection<IContentKey, ContentKey>
	{
		public ContentKeyCollection(CpixDocument document) : base(document)
		{
		}

		protected override string ContainerName => "ContentKeyList";

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, ContentKey entity)
		{
			throw new NotImplementedException();
		}

		protected override void ValidateEntityBeforeAdd(ContentKey entity)
		{
			if (this.Any(item => item.Id == entity.Id))
				throw new InvalidOperationException("The collection already contains a content key with the same ID.");

			if (!_document.ContentKeysAreReadable)
				throw new InvalidOperationException("New content keys cannot be added to a loaded CPIX document that contains encrypted content keys if you do not possess a delivery key.");

			entity.ValidateNewContentKey();
		}

		internal override void ValidateForSave()
		{
			throw new NotImplementedException();
		}
	}
}
