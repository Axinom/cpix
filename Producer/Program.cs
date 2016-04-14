using Axinom.Cpix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Producer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Loading certificates.");

			// If this is loaded from PFX, must be marked exportable due to funny behavior in .NET Framework.
			// Ideally, there should be no need to use an exportable key! But good enough for a sample.
			var signerCertificate = new X509Certificate2("Author1.pfx", "Author1", X509KeyStorageFlags.Exportable);

			var recipientCertificate1 = new X509Certificate2("Recipient1.cer");
			var recipientCertificate2 = new X509Certificate2("Recipient2.cer");

			Console.WriteLine("Preparing data for sample documents.");

			// Tuples of filename and CpixDocument structure to generate.
			var samples = new List<Tuple<string, CpixDocument>>();

			var document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			samples.Add(new Tuple<string, CpixDocument>("ClearKeys.xml", document));

			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddContentKeySignature(signerCertificate);
			document.SetDocumentSignature(signerCertificate);
			samples.Add(new Tuple<string, CpixDocument>("Signed.xml", document));

			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			samples.Add(new Tuple<string, CpixDocument>("Encrypted.xml", document));

			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			document.AddContentKeySignature(signerCertificate);
			document.SetDocumentSignature(signerCertificate);
			samples.Add(new Tuple<string, CpixDocument>("EncryptedAndSigned.xml", document));

			Console.WriteLine("Saving CPIX documents.");

			foreach (var sample in samples)
			{
				Console.WriteLine(sample.Item1);
				Console.WriteLine();

				using (var file = File.Create(sample.Item1))
					sample.Item2.Save(file);

				Console.WriteLine(File.ReadAllText(sample.Item1));

				Console.WriteLine();
			}

			Console.WriteLine("All done.");
		}

		private static ContentKey GenerateNewKey()
		{
			var key = new byte[16];
			_random.GetBytes(key);

			return new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = key
			};
		}

		private static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	}
}
