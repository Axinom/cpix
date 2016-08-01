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

		protected override string ContainerName => "ContentKeyUsageRuleList";

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, UsageRule entity)
		{
			throw new NotImplementedException();
		}
	}
}
