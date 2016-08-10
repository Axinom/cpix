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

			CryptographyHelpers.SignXmlElement(document, recipientsId, TestHelpers.PrivateAuthor1);
			CryptographyHelpers.SignXmlElement(document, contentKeysId, TestHelpers.PrivateAuthor1);
			CryptographyHelpers.SignXmlElement(document, usageRulesId, TestHelpers.PrivateAuthor1);
			CryptographyHelpers.SignXmlElement(document, usageRulesId, TestHelpers.PrivateAuthor2);
			CryptographyHelpers.SignXmlElement(document, "", TestHelpers.PrivateAuthor1);

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
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));
			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient2));
			TestHelpers.AddUsageRule(document);
			TestHelpers.AddUsageRule(document);

			// It will be saved without whitespace or formatting. This is fine.
			var buffer = new MemoryStream();
			document.Save(buffer);

			buffer.Position = 0;

			return buffer;
		}
	}
}
