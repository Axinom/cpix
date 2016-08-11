using Axinom.Cpix;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Xunit;

namespace Tests
{
	/// <summary>
	/// These tests try out various cases of unusual input that is valid CPIX but not of the sort produced
	/// by this library. The idea is to ensure that we are actually interoperable and read real CPIX, not "our CPIX".
	/// </summary>
	public sealed class UnusualInputTests
	{
		[Fact]
		public void LoadAndWriteAndReload_DocumentWithUnusualNamespacePrefixes_WorksJustFineAndDandy()
		{
			const string cpixPrefix = "hagihuaa44444";
			const string pskcPrefix = "se5o8jmmb7";
			const string xmlencPrefix = "sbearbwabbbbb";
			const string xmldsigPrefix = "h878r88919919199198919";
			const string xsiPrefix = "qqqqqq";
			const string xsdPrefix = "irfui";

			// We create a blank document that predefines some unusual namespaces prefixes, then make sure all still works.
			var xmlDocument = new XmlDocument();
			xmlDocument.AppendChild(xmlDocument.CreateElement(cpixPrefix, "CPIX", Constants.CpixNamespace));

			// We delcare a default namespace that will not be used for anything.
			var attribute = xmlDocument.CreateAttribute(null, "xmlns", Constants.XmlnsNamespace);
			attribute.Value = "http://nonsense.com.example.org";
			xmlDocument.DocumentElement.Attributes.Append(attribute);

			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xmldsigPrefix, Constants.XmlDigitalSignatureNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xmlencPrefix, Constants.XmlEncryptionNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, pskcPrefix, Constants.PskcNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xsiPrefix, "http://www.w3.org/2001/XMLSchema-instance");
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xsdPrefix, "http://www.w3.org/2001/XMLSchema");

			var buffer = new MemoryStream();
			xmlDocument.Save(buffer);

			// Now we have our document. Let's add stuff to it and see what happens.
			buffer.Position = 0;
			var document = CpixDocument.Load(buffer);
			FillDocumentWithData(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.ContentKeys.Count);
			Assert.Equal(2, document.Recipients.Count);
			Assert.Equal(2, document.UsageRules.Count);
		}

		[Fact]
		public void SignAndLoadAndSave_DocumentWithWhitespaceAndIndentation_DoesNotBreakSignatures()
		{
			// Signatures are sensitive to even whitespace changes. While this library deliberately avoids generating
			// whitespace to keep it simple, we cannot assume that all input is without whitespace.
			// The library must be capable of preserving signed parts of existing documents that contain whitespace.

			var cpixStream = GenerateNontrivialCpixStream();

			// Now create a nicely formatted copy.
			var formattedCpixStream = new MemoryStream();
			XmlHelpers.PrettyPrintXml(cpixStream, formattedCpixStream);

			// Now sign it!
			var document = new XmlDocument();
			document.PreserveWhitespace = true;

			formattedCpixStream.Position = 0;
			document.Load(formattedCpixStream);

			// Note that the collections are not given IDs as they were not signed on save.
			// We need to manually give them IDs. That's fine - we can also verify that we have no ID format dependencies!
			var namespaces = XmlHelpers.CreateCpixNamespaceManager(document);

			const string recipientsId = "id-for-recipients----";
			const string contentKeysId = "_id_for_content_keys";
			const string usageRulesId = "a.0a.0a.0a.0a.0a.a0.0a0.0404040......";

			SetElementId(document, namespaces, "/cpix:CPIX/cpix:DeliveryDataList", recipientsId);
			SetElementId(document, namespaces, "/cpix:CPIX/cpix:ContentKeyList", contentKeysId);
			SetElementId(document, namespaces, "/cpix:CPIX/cpix:ContentKeyUsageRuleList", usageRulesId);

			CryptographyHelpers.SignXmlElement(document, recipientsId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(document, contentKeysId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(document, usageRulesId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(document, usageRulesId, TestHelpers.Certificate2WithPrivateKey);
			CryptographyHelpers.SignXmlElement(document, "", TestHelpers.Certificate1WithPrivateKey);

			// Okay, that's fine. Save!
			var signedCpixStream = new MemoryStream();

			using (var writer = XmlWriter.Create(signedCpixStream, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				CloseOutput = false
			}))
			{
				document.Save(writer);
			}

			signedCpixStream.Position = 0;

			// Now it should be a nice valid and signed CPIX document.
			var cpix = CpixDocument.Load(signedCpixStream);

			Assert.NotNull(cpix.SignedBy);
			Assert.Equal(1, cpix.Recipients.SignedBy.Count());
			Assert.Equal(1, cpix.ContentKeys.SignedBy.Count());
			Assert.Equal(2, cpix.UsageRules.SignedBy.Count());

			// And save/load should preserve all the niceness.
			cpix = TestHelpers.Reload(cpix);

			Assert.NotNull(cpix.SignedBy);
			Assert.Equal(1, cpix.Recipients.SignedBy.Count());
			Assert.Equal(1, cpix.ContentKeys.SignedBy.Count());
			Assert.Equal(2, cpix.UsageRules.SignedBy.Count());

			// And, of course, the data should still be there.
			Assert.Equal(2, cpix.ContentKeys.Count);
			Assert.Equal(2, cpix.Recipients.Count);
			Assert.Equal(2, cpix.UsageRules.Count);

			// No exception? Success!
		}

		private static void SetElementId(XmlDocument document, XmlNamespaceManager namespaces, string xpathQuery, string id)
		{
			var element = (XmlElement)document.SelectSingleNode(xpathQuery, namespaces);
			element.SetAttribute("id", id);
		}

		private static MemoryStream GenerateNontrivialCpixStream()
		{
			var document = new CpixDocument();
			FillDocumentWithData(document);

			// It will be saved without whitespace or formatting. This is fine.
			var buffer = new MemoryStream();
			document.Save(buffer);

			buffer.Position = 0;

			return buffer;
		}

		private static void FillDocumentWithData(CpixDocument document)
		{
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));
			TestHelpers.AddUsageRule(document);
			TestHelpers.AddUsageRule(document);
		}
	}
}
