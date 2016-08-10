using Axinom.Cpix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tests
{
	static class TestHelpers
	{
		public static CpixDocument Reload(CpixDocument document, IReadOnlyCollection<X509Certificate2> decryptionCertificates = null)
		{
			var buffer = new MemoryStream();

			document.Save(buffer);
			buffer.Position = 0;

			return CpixDocument.Load(buffer, decryptionCertificates);
		}

		public static Tuple<Guid, byte[]> GenerateKeyData()
		{
			var key = new byte[16];
			Random.GetBytes(key);

			return new Tuple<Guid, byte[]>(Guid.NewGuid(), key);
		}

		public static ContentKey GenerateContentKey()
		{
			var keyData = GenerateKeyData();

			return new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			};
		}

		public static UsageRule AddUsageRule(CpixDocument document)
		{
			var contentKey = document.ContentKeys.First();

			var rule = new UsageRule
			{
				KeyId = contentKey.Id,

				// Some arbitrary filters here, just to generate interesting test data.
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MaxChannels = 5,
						MinChannels = 0
					}
				},
				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 100,
						MaxBitrate = 5198493
					},
					new BitrateFilter
					{
						MinBitrate = 5198494,
						MaxBitrate = 100000000000000
					}
				}
			};

			document.UsageRules.Add(rule);

			return rule;
		}

		public static void PopulateCollections(CpixDocument document)
		{
			document.Recipients.Add(new Recipient(PublicRecipient1));
			document.Recipients.Add(new Recipient(PublicRecipient2));

			var key1 = GenerateContentKey();
			var key2 = GenerateContentKey();

			document.ContentKeys.Add(key1);
			document.ContentKeys.Add(key2);

			AddUsageRule(document);
			AddUsageRule(document);

			// Sanity check.
			foreach (var collection in document.EntityCollections)
				if (collection.Count == 0)
					throw new Exception("TestHelpers need update - not all collections got populated!");
		}

		public static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

		public static readonly X509Certificate2 PublicAuthor1 = new X509Certificate2("Author1.cer");
		public static readonly X509Certificate2 PublicAuthor2 = new X509Certificate2("Author2.cer");
		public static readonly X509Certificate2 PublicRecipient1 = new X509Certificate2("Recipient1.cer");
		public static readonly X509Certificate2 PublicRecipient2 = new X509Certificate2("Recipient2.cer");

		public static readonly X509Certificate2 PrivateAuthor1 = new X509Certificate2("Author1.pfx", "Author1");
		public static readonly X509Certificate2 PrivateAuthor2 = new X509Certificate2("Author2.pfx", "Author2");
		public static readonly X509Certificate2 PrivateRecipient1 = new X509Certificate2("Recipient1.pfx", "Recipient1");
		public static readonly X509Certificate2 PrivateRecipient2 = new X509Certificate2("Recipient2.pfx", "Recipient2");
	}
}
