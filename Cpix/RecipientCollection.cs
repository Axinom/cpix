using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class RecipientCollection : EntityCollection<IRecipient, Recipient>
	{
		internal RecipientCollection(CpixDocument document) : base(document)
		{
		}

		protected override string ContainerName => "DeliveryDataList";

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, Recipient entity)
		{
			throw new NotImplementedException();
		}

		protected override void ValidateEntityBeforeAdd(Recipient entity)
		{
			throw new NotImplementedException();
		}

		internal override void ValidateForSave()
		{
			throw new NotImplementedException();
		}
	}
}
