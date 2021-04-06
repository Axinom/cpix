using Axinom.Cpix.Tests;
using System;
using System.IO;
using System.Linq;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class Complex : ITestVector
	{
		public string Description => "All types of entities, with many data fields filled, with encryption of content keys and with signatures on everything. The document as a whole is signed using Cert4 and each collection is signed using both Cert3 and Cert4.";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			const string complexLabel = "滆 柦柋牬 趉軨鄇 鶊鵱, 緳廞徲 鋑鋡髬 溮煡煟 綡蒚";

			document.SignedBy = TestHelpers.Certificate4WithPrivateKey;
			document.Recipients.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.Recipients.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.ContentKeys.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.ContentKeys.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.DrmSystems.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.DrmSystems.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.ContentKeyPeriods.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.ContentKeyPeriods.AddSignature(TestHelpers.Certificate4WithPrivateKey);
			document.UsageRules.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.UsageRules.AddSignature(TestHelpers.Certificate4WithPrivateKey);

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("b4c3188b-eddd-453d-9bc2-1cbca7566239"),
				Value = Convert.FromBase64String("b1pkxdNYqPxljV68gohWcw=="),
				ExplicitIv = Convert.FromBase64String("eMCi02KFz9fdUGd/6B+lgw=="),
				CommonEncryptionScheme = "cenc"
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("c6294999-5f48-445f-bcce-f7e5f736d7c6"),
				Value = Convert.FromBase64String("moOVrJvuhUUQ4LpPusAd5g=="),
				ExplicitIv = Convert.FromBase64String("XiLNTv8ZHbMI+F2g2TkL6w=="),
				CommonEncryptionScheme = "cenc"
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("b181a4df-2c38-41a4-993f-90b2f21343f6"),
				Value = Convert.FromBase64String("67gabJtKDWd2crHr+JQT1A=="),
				ExplicitIv = Convert.FromBase64String("E5XdJoK4ja+Rf/dcVvhRTA=="),
				CommonEncryptionScheme = "cenc"
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("a466cdfd-e556-4b1d-8098-c1a4aa78997a"),
				Value = Convert.FromBase64String("rRuRUWAibaUtai0qQnb71g=="),
				ExplicitIv = Convert.FromBase64String("yIZGwf6xwXgRHyXBNV0jRA=="),
				CommonEncryptionScheme = "cenc"
			});

			DrmSignalingHelpers.AddDefaultSignalingForAllKeys(document);

			document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Id = "keyperiod_1",
				Index = 1
			});
			document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Id = "keyperiod_2",
				Start = new DateTimeOffset(2020, 9, 4, 1, 1, 1, TimeSpan.Zero),
				End = new DateTimeOffset(2020, 9, 4, 2, 1, 1, TimeSpan.Zero)
			});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate2WithPublicKey));

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.First().Id,
				IntendedTrackType = "TrackType1",

				AudioFilters = new[]
				{
					new AudioFilter
					{
						MinChannels = 1,
						MaxChannels = 2
					},
					new AudioFilter
					{
						MinChannels = 8,
						MaxChannels = 10
					}
				},
				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 1000,
						MaxBitrate = 5 * 1000 * 1000
					},
					new BitrateFilter
					{
						MinBitrate = 10 * 1000 * 1000,
						MaxBitrate = 32 * 1000 * 1000
					}
				},
				LabelFilters = new[]
				{
					new LabelFilter("EncryptedStream"),
					new LabelFilter("CencStream"),
					new LabelFilter(complexLabel),
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.Last().Id,
				IntendedTrackType = "TrackType2",

				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 1000,
						MaxBitrate = 5 * 1000 * 1000
					},
					new BitrateFilter
					{
						MinBitrate = 10 * 1000 * 1000,
						MaxBitrate = 32 * 1000 * 1000
					}
				},
				LabelFilters = new[]
				{
					new LabelFilter("EncryptedStream"),
					new LabelFilter("CencStream"),
					new LabelFilter(complexLabel),
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
			});

			document.Save(outputStream);
		}
	}
}
