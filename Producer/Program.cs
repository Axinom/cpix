using Axinom.Cpix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Producer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Loading certificates.");

			var signerCertificate1 = new X509Certificate2("Author1.pfx", "Author1");
			var signerCertificate2 = new X509Certificate2("Author2.pfx", "Author2");

			var recipientCertificate1 = new X509Certificate2("Recipient1.cer");
			var recipientCertificate2 = new X509Certificate2("Recipient2.cer");

			Console.WriteLine("Preparing data for sample documents.");

			// Tuples of filename and CpixDocument structure to generate.
			var samples = new List<Tuple<string, CpixDocument>>();

			#region Example: ClearKeys.xml
			var document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			samples.Add(new Tuple<string, CpixDocument>("ClearKeys.xml", document));
			#endregion

			#region Example: Signed.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddContentKeySignature(signerCertificate1);
			document.SetDocumentSignature(signerCertificate1);
			samples.Add(new Tuple<string, CpixDocument>("Signed.xml", document));
			#endregion

			#region Example: Encrypted.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			samples.Add(new Tuple<string, CpixDocument>("Encrypted.xml", document));
			#endregion

			#region Example: EncryptedAndSigned.xml
			document = new CpixDocument();
			document.AddContentKey(GenerateNewKey());
			document.AddContentKey(GenerateNewKey());
			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			document.AddContentKeySignature(signerCertificate1);
			document.SetDocumentSignature(signerCertificate1);
			samples.Add(new Tuple<string, CpixDocument>("EncryptedAndSigned.xml", document));
			#endregion

			#region WithRulesAndEncryptedAndSigned.xml
			document = new CpixDocument();

			var lowValueKeyPeriod1 = GenerateNewKey();
			var highValueKeyPeriod1 = GenerateNewKey();
			var audioKey = GenerateNewKey();

			var periodDuration = TimeSpan.FromHours(1);
			var period1Start = new DateTimeOffset(2016, 6, 6, 6, 10, 0, TimeSpan.Zero);
			var period2Start = period1Start + periodDuration;

			document.AddContentKey(lowValueKeyPeriod1);
			document.AddContentKey(highValueKeyPeriod1);
			document.AddContentKey(audioKey);

			document.AddUsageRule(new UsageRule
			{
				KeyId = lowValueKeyPeriod1.Id,

				VideoFilter = new VideoFilter
				{
					MaxPixels = 1280 * 720 - 1
				}
			});

			document.AddUsageRule(new UsageRule
			{
				KeyId = highValueKeyPeriod1.Id,

				VideoFilter = new VideoFilter
				{
					MinPixels = 1280 * 720
				}
			});
			
			document.AddUsageRule(new UsageRule
			{
				KeyId = audioKey.Id,
				AudioFilter = new AudioFilter()
			});

			document.AddRecipient(recipientCertificate1);
			document.AddRecipient(recipientCertificate2);
			document.AddContentKeySignature(signerCertificate1);
			document.AddUsageRuleSignature(signerCertificate2);
			document.SetDocumentSignature(signerCertificate2);
			samples.Add(new Tuple<string, CpixDocument>("WithRulesAndEncryptedAndSigned.xml", document)); 
			#endregion

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
