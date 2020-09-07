using Axinom.Cpix.Tests;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class EvenMoreComplex : ITestVector
	{
		public string Description => @"A more complex version of the ""Complex"" test vector! All types of entities, with many data fields filled, with encryption of content keys and with signatures on everything. The document as a whole is signed using Cert4 and each collection is signed using both Cert3 and Cert4.

In addition, nonstandard namespace prefixes are used everywhere, the XML is pretty-printed before signing, the elements to be signed are given unusual id values and various XML comments are added after signing. Finally, the whole thing is encoded using UTF-16.

The resulting output is still valid and all the signatures should successfully pass verification on a conforming implementation!";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			// Implementation borrows heavily from UnusualInputTests.

			const string cpixPrefix = "aa";
			const string pskcPrefix = "bb";
			const string xmlencPrefix = "cc";
			const string xmldsigPrefix = "dd";
			const string xsiPrefix = "ee";
			const string xsdPrefix = "ff";

			// We create a blank document that predefines the unusual namespaces prefixes.
			var xmlDocument = new XmlDocument();
			xmlDocument.AppendChild(xmlDocument.CreateElement(cpixPrefix, "CPIX", Constants.CpixNamespace));

			// We delcare a default namespace that will not be used for anything.
			var attribute = xmlDocument.CreateAttribute(null, "xmlns", Constants.XmlnsNamespace);
			attribute.Value = "⚽";
			xmlDocument.DocumentElement.Attributes.Append(attribute);

			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xmldsigPrefix, Constants.XmlDigitalSignatureNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xmlencPrefix, Constants.XmlEncryptionNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, pskcPrefix, Constants.PskcNamespace);
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xsiPrefix, "http://www.w3.org/2001/XMLSchema-instance");
			XmlHelpers.DeclareNamespace(xmlDocument.DocumentElement, xsdPrefix, "http://www.w3.org/2001/XMLSchema");

			var buffer = new MemoryStream();
			xmlDocument.Save(buffer);

			buffer.Position = 0;
			// Loading the blank document means we will now use the above prefixes.
			var document = CpixDocument.Load(buffer);

			const string complexLabel = "滆 柦柋牬 趉軨鄇 鶊鵱, 緳廞徲 鋑鋡髬 溮煡煟 綡蒚";

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("152ae2e0-f455-486e-81d1-6df5fc5d7179"),
				Value = Convert.FromBase64String("B+DoDP4r/j1NEr7b2aKXlw=="),
				ExplicitIv = Convert.FromBase64String("7qFwxoV85KBUNVXw8rgY3w==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("0cbe1c84-5c54-4ce8-8893-ff77f7d793e1"),
				Value = Convert.FromBase64String("CedOXHsXc3xQ+HQuJOJZ+g=="),
				ExplicitIv = Convert.FromBase64String("zm4oYmRJWbLPrqXnhFS00w==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("486a8d08-29f7-42f5-9a9a-a1ab9b0685ad"),
				Value = Convert.FromBase64String("ADq3douHS0QrY1omNB1njA=="),
				ExplicitIv = Convert.FromBase64String("wS4WCf4LgOe45BuG/qv5LA==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("84044421-a871-4999-8931-289aa6f4a607"),
				Value = Convert.FromBase64String("JlZWL6tfkh6e8k9U1IOC8A=="),
				ExplicitIv = Convert.FromBase64String("9tlEFBcaX2S8/tfgQw9vAQ==")
			});

			DrmSignalingHelpers.AddDefaultSignalingForAllKeys(document);

			document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Id = "keyperiod_1",
				Index = 1
			});
			document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Id = "keyperiod_2",
				Start = new DateTime(2020, 9, 4, 1, 1, 1),
				End = new DateTime(2020, 9, 4, 2, 1, 1)
			});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate2WithPublicKey));

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.First().Id,

				AudioFilters = new[]
				{
					new AudioFilter
					{
						MinChannels = 1,
						MaxChannels = 2
					},
					new AudioFilter
					{
						MinChannels = 8,
						MaxChannels = 10
					}
				},
				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 1000,
						MaxBitrate = 5 * 1000 * 1000
					},
					new BitrateFilter
					{
						MinBitrate = 10 * 1000 * 1000,
						MaxBitrate = 32 * 1000 * 1000
					}
				},
				LabelFilters = new[]
				{
					new LabelFilter("EncryptedStream"),
					new LabelFilter("CencStream"),
					new LabelFilter(complexLabel),
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.Last().Id,

				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 1000,
						MaxBitrate = 5 * 1000 * 1000
					},
					new BitrateFilter
					{
						MinBitrate = 10 * 1000 * 1000,
						MaxBitrate = 32 * 1000 * 1000
					}
				},
				LabelFilters = new[]
				{
					new LabelFilter("EncryptedStream"),
					new LabelFilter("CencStream"),
					new LabelFilter(complexLabel),
				},
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MinPixels = 1000,
						MaxPixels = 1920 * 1080,
						MinFramesPerSecond = 10,
						MaxFramesPerSecond = 30,
						WideColorGamut = false,
						HighDynamicRange = true,
					},
					new VideoFilter
					{
						MinPixels = 1000,
						MaxPixels = 4096 * 4096,
						MinFramesPerSecond = 30,
						MaxFramesPerSecond = 200,
						WideColorGamut = false,
						HighDynamicRange = false,
					}
				}
			});

			// Save the XML, using funny prefixes and with complex data.
			buffer.SetLength(0);
			document.Save(buffer);

			// Now pretty-print the XML.
			var formattedStream = new MemoryStream();
			buffer.Position = 0;
			XmlHelpers.PrettyPrintXml(buffer, formattedStream);
			buffer = formattedStream;

			// Now modify the element IDs to be signed, sign the document, add comments and save as UTF-16.
			xmlDocument = new XmlDocument();
			xmlDocument.PreserveWhitespace = true;

			buffer.Position = 0;
			xmlDocument.Load(buffer);

			var namespaces = XmlHelpers.CreateCpixNamespaceManager(xmlDocument);

			const string recipientsId = "id-for-recipients----";
			const string contentKeysId = "_id_for_content_keys";
			const string drmSystemsId = "_id_for_drm_systems";
			const string contentKeyPeriodsId = "_id_for_content_key_periods";
			const string usageRulesId = "a.0a.0a.0a.0a.0a.a0.0a0.0404040......";

			UnusualInputTests.SetElementId(xmlDocument, namespaces, "/cpix:CPIX/cpix:DeliveryDataList", recipientsId);
			UnusualInputTests.SetElementId(xmlDocument, namespaces, "/cpix:CPIX/cpix:ContentKeyList", contentKeysId);
			UnusualInputTests.SetElementId(xmlDocument, namespaces, "/cpix:CPIX/cpix:DRMSystemList", drmSystemsId);
			UnusualInputTests.SetElementId(xmlDocument, namespaces, "/cpix:CPIX/cpix:ContentKeyPeriodList", contentKeyPeriodsId);
			UnusualInputTests.SetElementId(xmlDocument, namespaces, "/cpix:CPIX/cpix:ContentKeyUsageRuleList", usageRulesId);

			CryptographyHelpers.SignXmlElement(xmlDocument, recipientsId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, contentKeysId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, drmSystemsId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, contentKeyPeriodsId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, usageRulesId, TestHelpers.Certificate1WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, usageRulesId, TestHelpers.Certificate2WithPrivateKey);
			CryptographyHelpers.SignXmlElement(xmlDocument, "", TestHelpers.Certificate1WithPrivateKey);

			// Add comments everywhere.
			namespaces = XmlHelpers.CreateCpixNamespaceManager(xmlDocument);

			UnusualInputTests.AddCommentAsChild(xmlDocument.DocumentElement);

			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DeliveryDataList", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DRMSystemList", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyPeriodList", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyUsageRuleList", namespaces));

			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DeliveryDataList/cpix:DeliveryData", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList/cpix:ContentKey", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:DRMSystemList/cpix:DRMSystem", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyPeriodList/cpix:ContentKeyPeriod", namespaces));
			UnusualInputTests.AddCommentAsChild((XmlElement)xmlDocument.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyUsageRuleList/cpix:ContentKeyUsageRule", namespaces));

			// Save the signed document as UTF-16.
			using (var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
			{
				Encoding = Encoding.Unicode,
				CloseOutput = false
			}))
			{
				xmlDocument.Save(writer);
			}

			// Phew. That's enough for now.
		}
	}
}
