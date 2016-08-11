using Axinom.Cpix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tests
{
	internal static class TestHelpers
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
				},
				LabelFilters = new[]
				{
					new LabelFilter("aaaaa"),
					new LabelFilter("bbbb"),
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
			};

			document.UsageRules.Add(rule);

			return rule;
		}

		public static void PopulateCollections(CpixDocument document)
		{
			document.Recipients.Add(new Recipient(Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(Certificate4WithPublicKey));

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

		// makecert -pe -n "CN=CPIX Example Entity 1" -sky exchange -a sha512 -len 4096 -r -ss My
		public static readonly X509Certificate2 Certificate1WithPublicKey = new X509Certificate2("Cert1.cer");
		public static readonly X509Certificate2 Certificate2WithPublicKey = new X509Certificate2("Cert2.cer");
		public static readonly X509Certificate2 Certificate3WithPublicKey = new X509Certificate2("Cert3.cer");
		public static readonly X509Certificate2 Certificate4WithPublicKey = new X509Certificate2("Cert4.cer");

		public static readonly X509Certificate2 Certificate1WithPrivateKey = new X509Certificate2("Cert1.pfx", "Cert1");
		public static readonly X509Certificate2 Certificate2WithPrivateKey = new X509Certificate2("Cert2.pfx", "Cert2");
		public static readonly X509Certificate2 Certificate3WithPrivateKey = new X509Certificate2("Cert3.pfx", "Cert3");
		public static readonly X509Certificate2 Certificate4WithPrivateKey = new X509Certificate2("Cert4.pfx", "Cert4");

		public static readonly X509Certificate2 WeakSha1CertificateWithPublicKey = new X509Certificate2("WeakCert_Sha1.cer");
		public static readonly X509Certificate2 WeakSha1CertificateWithPrivateKey = new X509Certificate2("WeakCert_Sha1.pfx", "WeakCert_Sha1");

		public static readonly X509Certificate2 WeakSmallKeyCertificateWithPublicKey = new X509Certificate2("WeakCert_SmallKey.cer");
		public static readonly X509Certificate2 WeakSmallKeyCertificateWithPrivateKey = new X509Certificate2("WeakCert_SmallKey.pfx", "WeakCert_SmallKey");
	}
}
