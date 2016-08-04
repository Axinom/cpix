using System;
using System.IO;
using System.Linq;
using System.Xml;
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

		internal static XmlElement InsertTopLevelCpixXmlElementInCorrectOrder(XmlElement element, XmlDocument document)
		{
			// If this returns null, our element should be the first one.
			var insertAfter = TryDetectInsertAfterElementForCpixTopLevelInsertion(element, document);

			if (insertAfter == null)
				return (XmlElement)document.DocumentElement.PrependChild(element);
			else
				return (XmlElement)document.DocumentElement.InsertAfter(element, insertAfter);
		}

		private static XmlElement TryDetectInsertAfterElementForCpixTopLevelInsertion(XmlElement element, XmlDocument document)
		{
			// Top-level elements have a specific order they need to be in! We insert in the appropriate order.

			var theseComeBefore = Constants.TopLevelXmlElementOrder
				.TakeWhile(item => item.Item1 != element.LocalName || item.Item2 != element.NamespaceURI)
				.ToArray();

			// If we got everything, this means the current element is unknown and we have a defect!
			if (theseComeBefore.Length == Constants.TopLevelXmlElementOrder.Length)
				throw new ArgumentException("The correct ordering of this element in a CPIX document cannot be determined as the element is unknown.", nameof(element));

			if (theseComeBefore.Length == 0)
				return null; // This is the first element.

			// If there already exist elements of the same type, add after the latest of the same type.
			var insertAfter = document.DocumentElement.ChildNodes.OfType<XmlElement>()
				.LastOrDefault(e => e.LocalName == element.LocalName && e.NamespaceURI == element.NamespaceURI);

			if (insertAfter != null)
				return insertAfter;

			// Otherwise, add after whatever we detect should come right before us.
			// We start scanning from the last one, obviously.
			foreach (var candidate in theseComeBefore.Reverse())
			{
				insertAfter = document.DocumentElement.ChildNodes.OfType<XmlElement>()
					.LastOrDefault(e => e.LocalName == candidate.Item1 && e.NamespaceURI == candidate.Item2);

				if (insertAfter != null)
					return insertAfter;
			}

			// None of the "come before" exist? Then our element is the first one!
			return null;
		}
	}
}
