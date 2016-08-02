using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class UsageRuleCollection : EntityCollection<IUsageRule, UsageRule>
	{
		public UsageRuleCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => "ContentKeyUsageRuleList";

		protected override UsageRule DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			throw new NotImplementedException();
		}

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, UsageRule entity)
		{
			throw new NotImplementedException();
		}
	}
}
