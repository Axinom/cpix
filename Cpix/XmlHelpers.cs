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

		internal static void ValidateDocumentAgainstSchema(Stream documentStream, XmlSchemaSet schemaSet)
		{
			// Validating much be done as a separate XmlDocument, not associated with the "normal" one
			// because the presence of schema information causes Canonical XML processing to be incorrect,
			// leading to failures to verify and/or generate digital signatures.

			var settings = new XmlReaderSettings
			{
				ValidationType = ValidationType.None,
				CloseInput = false
			};

			settings.Schemas.Add(schemaSet);

			// Read from the start. We don't support any fancy scenarios.
			documentStream.Position = 0;

			var document = new XmlDocument();

			using (var reader = XmlReader.Create(documentStream, settings))
				document.Load(reader);

			// Reset for the next guy, just to be nice.
			documentStream.Position = 0;
		}
	}
}
