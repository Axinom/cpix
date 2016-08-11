using Axinom.Cpix;
using System.IO;
using System.Text;
using System.Xml;
using Tests;

namespace TestVectorGenerator
{
	sealed class Invalid_BadDocumentSignature : ITestVector
	{
		public string Description => @"The document signature should fail validation because an extra namespace declaration attribute has been added to the document root element.";
		public bool OutputIsValid => false;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.SignedBy = TestHelpers.Certificate2WithPrivateKey;

			var buffer = new MemoryStream();
			document.Save(buffer);

			var xml = new XmlDocument();
			buffer.Position = 0;
			xml.Load(buffer);

			XmlHelpers.DeclareNamespace(xml.DocumentElement, "xxx", "http://example.com/this/makes/the/document/signature/invalid");

			using (var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				CloseOutput = false
			}))
			{
				xml.Save(writer);
			}
		}
	}
}
