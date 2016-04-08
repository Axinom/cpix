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
			var signerCertificate = new X509Certificate2("Author.pfx", "Author", X509KeyStorageFlags.Exportable);

			var recipientCertificate1 = new X509Certificate2("Recipient1.cer");
			var recipientCertificate2 = new X509Certificate2("Recipient2.cer");

			Console.WriteLine("Preparing data for sample documents.");

			// Tuples of filename and CpixDocument structure to generate.
			var samples = new List<Tuple<string, CpixDocument>>
			{
				// Just two content keys, that's all. No encryption, no nothing.
				new Tuple<string, CpixDocument>( "UC1-KeyDelivery.xml", new CpixDocument
				{
					Keys =
					{
						GenerateNewKey(),
						GenerateNewKey()
					}
				}),
				// Now with authentication (digital signature).
				new Tuple<string, CpixDocument>( "UC2-Signed.xml", new CpixDocument
				{
					Signer = signerCertificate,
					Keys =
					{
						GenerateNewKey(),
						GenerateNewKey()
					}
				}),
				// Now with encryption.
				new Tuple<string, CpixDocument>( "UC3-Encrypted.xml", new CpixDocument
				{
					Recipients =
					{
						recipientCertificate1,
						recipientCertificate2
					},
					Keys =
					{
						GenerateNewKey(),
						GenerateNewKey()
					}
				}),
				// Now with both!
				new Tuple<string, CpixDocument>( "UC2+UC3-SignedAndEncrypted.xml", new CpixDocument
				{
					Signer = signerCertificate,
					Recipients =
					{
						recipientCertificate1,
						recipientCertificate2
					},
					Keys =
					{
						GenerateNewKey(),
						GenerateNewKey()
					}
				}),
			};

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
