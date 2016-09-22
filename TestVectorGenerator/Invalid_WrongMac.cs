using Axinom.Cpix.Tests;
using System.IO;
using System.Text;
using System.Xml;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class Invalid_WrongMac : ITestVector
	{
		public string Description => @"The MAC on the encrypted content key is invalid! Expected implementation behavior:

* Loading should fail when the recipient (Cert1) tries to decrypt the content key.
* Loading should be successful if no attempt is made to decrypt the content key, as the error can only be discovered during decryption.";
		public bool OutputIsValid => false;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));

			var buffer = new MemoryStream();
			document.Save(buffer);

			var xml = new XmlDocument();
			buffer.Position = 0;
			xml.Load(buffer);

			var namespaces = XmlHelpers.CreateCpixNamespaceManager(xml);
			var mac = xml.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList/cpix:ContentKey/cpix:Data/pskc:Secret/pskc:ValueMAC", namespaces);

			// No way this will be the right MAC!
			mac.InnerText = "YtijEC7siGSqLg/9WrZ5Z7/TCVE9BydVO9UOv28yZr5+QCdstz8uAQvC9mFFx8hag0LKw461/OKIe5Fr7Mvo2A==";

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
