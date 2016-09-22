using Axinom.Cpix.Tests;
using System.IO;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class EncryptedContentKeysWithMultipleRecipients : ITestVector
	{
		public string Description => "Content keys encrypted for delivery to four recipients (Cert1 through Cert4).";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate2WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));

			document.Save(outputStream);
		}
	}
}
