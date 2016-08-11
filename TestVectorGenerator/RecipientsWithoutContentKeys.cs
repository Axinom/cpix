using Axinom.Cpix;
using System.IO;
using Tests;

namespace TestVectorGenerator
{
	sealed class RecipientsWithoutContentKeys : ITestVector
	{
		public string Description => "Defines a few authorized recipients (Cert3 and Cert4) and delivery data but no actual content keys to deliver.";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));

			document.Save(outputStream);
		}
	}
}
