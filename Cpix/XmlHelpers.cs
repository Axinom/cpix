using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Axinom.Cpix
{
	static class XmlHelpers
	{
		/// <summary>
		/// XML-deserializes an object of type T from an XmlElement.
		/// </summary>
		internal static T Deserialize<T>(XmlElement element)
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

		/// <summary>
		/// Appends a child element XML-serialized from an object of type T and reuses already-declared namespaces in doing so.
		/// </summary>
		internal static XmlElement AppendChildAndReuseNamespaces<T>(T xmlObject, XmlDocument document, XmlElement parent)
		{
			// We have a little problem here. See, XmlSerializer generates full documents, which means that
			// it will declare the namespaces on absolutely everything it genreates. This causes a lot of
			// redundancy and spam. We need to reuse the namespaces as far as possible.

			// The navigator gives us access to the namespaces that are in scope at the parent element.
			var parentNavigator = parent.CreateNavigator();
			var namespaces = parentNavigator.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);

			var serialized = CreateXmlElementAndReuseNamespaces(xmlObject, namespaces);

			return (XmlElement)parent.AppendChild(document.ImportNode(serialized, true));
		}

		/// <summary>
		/// Transforms an object of type T to an XmlElement via XML-serialization,
		/// reusing existing namespaces instead of re-declaring them.
		/// </summary>
		internal static XmlElement CreateXmlElementAndReuseNamespaces<T>(T xmlObject, IDictionary<string, string> namespacesToReuse)
		{
			if (xmlObject == null)
				throw new ArgumentNullException(nameof(xmlObject));

			if (namespacesToReuse == null)
				throw new ArgumentNullException(nameof(namespacesToReuse));

			using (var intermediateXmlBuffer = new MemoryStream())
			{
				var serializer = new XmlSerializer(typeof(T));

				// We create a temporary container element for serializing the object.
				// This container element will delcare all the relevant namespaces exactly
				// the same as they exist in the scope of the to-be-parent-element (provided
				// in namespacesToReuse) without re-defining them as is normal behavior.
				// Then we just extract the XmlElement and discard the container.
				// Very roundabout way but it is pretty much the only way to achieve desired behavior!

				using (var writer = XmlWriter.Create(intermediateXmlBuffer, new XmlWriterSettings
				{
					Encoding = Encoding.UTF8,
					CloseOutput = false,

					// We will be lazy and let the writer close up the container.
					WriteEndDocumentOnClose = true
				}))
				{
					var namespaces = new XmlSerializerNamespaces();

					// Dummy namespace used for the container itself, to avoid conflicts.
					// Hardcoded value but likelyhood of conflict is zero, barring deliberately designed conflicting data.
					writer.WriteStartElement("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "Container", "http://dummy.example.com");

					foreach (var ns in namespacesToReuse)
					{
						namespaces.Add(ns.Key, ns.Value);

						if (string.IsNullOrEmpty(ns.Key))
						{
							// Default namespace.
							writer.WriteAttributeString(null, "xmlns", Constants.XmlnsNamespace, ns.Value);
						}
						else
						{
							// Prefixed namespace.
							writer.WriteAttributeString("xmlns", ns.Key, Constants.XmlnsNamespace, ns.Value);
						}
					}

					// This will reuse all the namespaces we have inherited.
					serializer.Serialize(writer, xmlObject, namespaces);
				}

				// Seek back to beginning to load contents into XmlDocument.
				intermediateXmlBuffer.Position = 0;

				var xmlDocument = new XmlDocument();
				xmlDocument.Load(intermediateXmlBuffer);

				// Rip out the actual element from the container and return it.
				return xmlDocument.DocumentElement.ChildNodes.OfType<XmlElement>().Single();
			}
		}

		/// <summary>
		/// XML-serializes an object of type T, returning it as an XmlDocument.
		/// </summary>
		internal static XmlDocument Serialize<T>(T xmlObject)
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

		/// <summary>
		/// Top-level CPIX elements must be inserted to the document in a specific order, as the CPIX document
		/// is strictly ordered. This method will automatically insert elements in the correct order.
		/// </summary>
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

		/// <summary>
		/// Loads, reformats/indents and saves an XML document. Used primarily just for testing with formatted XML.
		/// </summary>
		internal static void PrettyPrintXml(Stream input, Stream output)
		{
			var document = new XmlDocument();
			document.PreserveWhitespace = true;
			document.Load(input);

			using (var writer = XmlWriter.Create(output, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true,
				IndentChars = "\t",
				CloseOutput = false
			}))
			{
				document.Save(writer);
			}
		}

		/// <summary>
		/// Creates a namespace manager with some convenient namespaces predefined.
		/// </summary>
		internal static XmlNamespaceManager CreateCpixNamespaceManager(XmlDocument document)
		{
			var manager = new XmlNamespaceManager(document.NameTable);
			manager.AddNamespace("cpix", Constants.CpixNamespace);
			manager.AddNamespace("pskc", Constants.PskcNamespace);
			manager.AddNamespace("enc", Constants.XmlEncryptionNamespace);
			manager.AddNamespace("ds", Constants.XmlDigitalSignatureNamespace);

			return manager;
		}
	}
}
