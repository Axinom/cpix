using System.Xml;
using Axinom.Cpix.Internal;

namespace Axinom.Cpix
{
	sealed class ContentKeyPeriodCollection : EntityCollection<ContentKeyPeriod>
	{
		public const string ContainerXmlElementName = "ContentKeyPeriodList";

		public ContentKeyPeriodCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => ContainerXmlElementName;

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, ContentKeyPeriod entity)
		{
			var contentKeyPeriodElement = new ContentKeyPeriodElement
			{
				Id = entity.Id,
				Index = entity.Index,
				Start = entity.Start,
				End = entity.End
			};

			return XmlHelpers.AppendChildAndReuseNamespaces(contentKeyPeriodElement, container);
		}

		protected override ContentKeyPeriod DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			var contentKeyPeriodElement = XmlHelpers.Deserialize<ContentKeyPeriodElement>(element);

			var contentKeyPeriod = new ContentKeyPeriod
			{
				Id = contentKeyPeriodElement.Id,
				Index = contentKeyPeriodElement.Index,
				Start = contentKeyPeriodElement.Start,
				End = contentKeyPeriodElement.End
			};

			return contentKeyPeriod;
		}
	}
}