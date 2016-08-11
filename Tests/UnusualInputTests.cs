using Axinom.Cpix;
using System;
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
		public void LoadDocument_WithXmlCommentsAddedAfterSigning_SuccessfullyValidatesSignature()
		{
			// The canonicalization we use excludes comments, so comments should have no effect on signature validity.

			var document = new CpixDocument();
			FillDocumentWithData(document);

			document.Recipients.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.ContentKeys.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.UsageRules.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.SignedBy = TestHelpers.Certificate3WithPrivateKey;

			var buffer = new MemoryStream();
			document.Save(buffer);

			// Let's now sprinkle comments all over the place.
			var xmlDocument = new XmlDocument();

			buffer.Position = 0;
			xmlDocument.Load(buffer);

			var namespaces = XmlHelpers.CreateCpixNamespaceManager(xmlDocument);

			AddCommentAsChild(xmlDocument.DocumentElement);

			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DeliveryDataList", namespaces));
			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList", namespaces));
			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyUsageRuleList", namespaces));

			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DeliveryDataList/cpix:DeliveryData", namespaces));
			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList/cpix:ContentKey", namespaces));
			AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyUsageRuleList/cpix:ContentKeyUsageRule", namespaces));

			buffer.SetLength(0);

			using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				CloseOutput = false,
			}))
			{
				xmlDocument.Save(writer);
			}

			buffer.Position = 0;
			document = CpixDocument.Load(buffer);

			Assert.NotNull(document.SignedBy);
			Assert.Equal(1, document.Recipients.SignedBy.Count());
			Assert.Equal(1, document.ContentKeys.SignedBy.Count());
			Assert.Equal(1, document.UsageRules.SignedBy.Count());

			// And, of course, the data should still be there.
			Assert.Equal(2, document.ContentKeys.Count);
			Assert.Equal(2, document.Recipients.Count);
			Assert.Equal(2, document.UsageRules.Count);
		}

		private static void AddCommentAsChild(XmlElement element)
		{
			element.AppendChild(element.OwnerDocument.CreateComment(Guid.NewGuid().ToString()));
		}

		[Fact]
		public void LoadDocument_WithUtf16EncodedInput_Succeeds()
		{
			// We ensure that some non-ASCII text survives the encoding/decoding/signing process intact.
			const string canary = "滆 柦柋牬 趉軨鄇 鶊鵱, 緳廞徲 鋑鋡髬 溮煡煟 綡蒚";

			var document = new CpixDocument();
			FillDocumentWithData(document);

			document.UsageRules.First().LabelFilters = new[]
			{
				new LabelFilter
				{
					Label = canary
				}
			};

			document.Recipients.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.ContentKeys.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.UsageRules.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.SignedBy = TestHelpers.Certificate3WithPrivateKey;

			var buffer = new MemoryStream();
			document.Save(buffer);

			// Now we have a basic UTF-8 document in the buffer. Convert to UTF-16!
			// Using XmlDocument here to do it in a "smart" way with all the XML processing.
			var xmlDocument = new XmlDocument();

			buffer.Position = 0;
			xmlDocument.Load(buffer);

			buffer.SetLength(0);

			using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings
			{
				Encoding = Encoding.Unicode,
				CloseOutput = false,
			}))
			{
				xmlDocument.Save(writer);
			}

			buffer.Position = 0;

			// Okay, does it load? It should!
			document = CpixDocument.Load(buffer);

			Assert.Equal(2, document.ContentKeys.Count);
			Assert.Equal(2, document.Recipients.Count);
			Assert.Equal(2, document.UsageRules.Count);

			Assert.Equal(canary, document.UsageRules.First().LabelFilters.Single().Label);
		}

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
