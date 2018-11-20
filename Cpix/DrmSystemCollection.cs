using Axinom.Cpix.Internal;
using System;
using System.Linq;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class DrmSystemCollection : EntityCollection<DrmSystem>
	{
		public const string ContainerXmlElementName = "DRMSystemList";

		public DrmSystemCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => ContainerXmlElementName;

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, DrmSystem entity)
		{
			var drmSystemElement = new DrmSystemElement
			{
				SystemId = entity.Id,
				KeyId = entity.KeyId,
				ContentProtectionData = entity.ContentProtectionData
			};

			return XmlHelpers.AppendChildAndReuseNamespaces(drmSystemElement, container);
		}

		protected override DrmSystem DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			var drmSystemElement = XmlHelpers.Deserialize<DrmSystemElement>(element);

			return new DrmSystem
			{
				Id = drmSystemElement.SystemId,
				KeyId = drmSystemElement.KeyId,
				ContentProtectionData = drmSystemElement.ContentProtectionData
			};
		}

		protected override void ValidateCollectionStateBeforeAdd(DrmSystem entity)
		{
			if (this.Any(s => s.Id == entity.Id && s.KeyId == entity.KeyId))
				throw new InvalidOperationException("The collection already contains a DRM system with the same ID and content key ID combination.");

			base.ValidateCollectionStateBeforeAdd(entity);
		}
	}
}