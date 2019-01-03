using Axinom.Cpix.Tests;
using System;
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

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("bc365b99-0667-446f-b417-ff0398c9a4c4"),
				Value = Convert.FromBase64String("gMMdXMudvuGpYW5k3lzf/g==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("1e25f2a7-76a9-4570-bc1a-d8181800d529"),
				Value = Convert.FromBase64String("WUxnvQjGw28bA3cgW/1jfg==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("2e4e6c21-c0d7-4a1c-80af-bff3d7cc5270"),
				Value = Convert.FromBase64String("cCudMIPMQkak1l+oCVXT2A==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("8ad35bd4-53ab-437b-8f42-6e1ea8e2f0d8"),
				Value = Convert.FromBase64String("zqvOfAja51IUSRV385bvoA==")
			});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate2WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));

			document.Save(outputStream);
		}
	}
}
