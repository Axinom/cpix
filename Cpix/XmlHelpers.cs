using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Axinom.Cpix
{
	static class XmlHelpers
	{
		internal static T XmlElementToXmlDeserialized<T>(XmlElement element)
		{
			using (var buffer = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(buffer))
					element.WriteTo(writer);

				buffer.Position = 0;

				var serializer = new XmlSerializer(typeof(T));
				return (T)serializer.Deserialize(buffer);
			}
		}

		internal static XmlElement InsertXmlObject<T>(T xmlObject, XmlDocument document, XmlElement parent)
		{
			return (XmlElement)parent.AppendChild(document.ImportNode(XmlObjectToXmlDocument(xmlObject).DocumentElement, true));
		}

		internal static XmlDocument XmlObjectToXmlDocument<T>(T xmlObject)
		{
			using (var intermediateXmlBuffer = new MemoryStream())
			{
				var serializer = new XmlSerializer(typeof(T));
				serializer.Serialize(intermediateXmlBuffer, xmlObject);

				// Seek back to beginning to load contents into XmlDocument.
				intermediateXmlBuffer.Position = 0;

				var xmlDocument = new XmlDocument();
				xmlDocument.Load(intermediateXmlBuffer);

				return xmlDocument;
			}
		}
	}
}
