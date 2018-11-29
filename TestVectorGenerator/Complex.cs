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
			document.UsageRules.AddSignature(TestHelpers.Certificate3WithPrivateKey);
			document.UsageRules.AddSignature(TestHelpers.Certificate4WithPrivateKey);

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			// Adding a non-random key, so its ID would match the key ID in the DRM
			// system signaling data fields.
			var contentKeyWithKnownId = new ContentKey
			{
				Id = Guid.Parse("f8c80c25-690f-4736-8132-430e5c6994ce"),
				Value = new byte[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5 }
			};
			document.ContentKeys.Add(contentKeyWithKnownId);

			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("edef8ba9-79d6-4ace-a3c8-27dcd51d21ed"),
				KeyId = contentKeyWithKnownId.Id,
				ContentProtectionData = "<cenc:pssh xmlns:cenc=\"urn:mpeg:cenc:2013\">AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==</cenc:pssh>",
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "#EXT-X-SESSION-KEY:METHOD=SAMPLE-AES,URI=\"data:text/plain;base64,AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==\",KEYID=0xF8C80C25690F47368132430E5C6994CE,KEYFORMAT=\"urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\",KEYFORMATVERSIONS=\"1\"",
					VariantPlaylistData = "#EXT-X-KEY:METHOD=SAMPLE-AES,URI=\"data:text/plain;base64,AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==\",KEYID=0xF8C80C25690F47368132430E5C6994CE,KEYFORMAT=\"urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\",KEYFORMATVERSIONS=\"1\""
				}
			});
			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("9a04f079-9840-4286-ab92-e65be0885f95"),
				KeyId = contentKeyWithKnownId.Id,
				ContentProtectionData =
					"<cenc:pssh xmlns:cenc=\"urn:mpeg:cenc:2013\">AAAB5HBzc2gAAAAAmgTweZhAQoarkuZb4IhflQAAAcTEAQAAAQABALoBPABXAFIATQBIAEUAQQBEAEUAUgAgAHgAbQBsAG4AcwA9ACIAaAB0AHQAcAA6AC8ALwBzAGMAaABlAG0AYQBzAC4AbQBpAGMAcgBvAHMAbwBmAHQALgBjAG8AbQAvAEQAUgBNAC8AMgAwADAANwAvADAAMwAvAFAAbABhAHkAUgBlAGEAZAB5AEgAZQBhAGQAZQByACIAIAB2AGUAcgBzAGkAbwBuAD0AIgA0AC4AMAAuADAALgAwACIAPgA8AEQAQQBUAEEAPgA8AFAAUgBPAFQARQBDAFQASQBOAEYATwA+ADwASwBFAFkATABFAE4APgAxADYAPAAvAEsARQBZAEwARQBOAD4APABBAEwARwBJAEQAPgBBAEUAUwBDAFQAUgA8AC8AQQBMAEcASQBEAD4APAAvAFAAUgBPAFQARQBDAFQASQBOAEYATwA+ADwASwBJAEQAPgBKAFEAegBJACsAQQA5AHAATgBrAGUAQgBNAGsATQBPAFgARwBtAFUAegBnAD0APQA8AC8ASwBJAEQAPgA8AC8ARABBAFQAQQA+ADwALwBXAFIATQBIAEUAQQBEAEUAUgA+AA==</cenc:pssh>" +
					"<pro xmlns=\"urn:microsoft:playready\">xAEAAAEAAQC6ATwAVwBSAE0ASABFAEEARABFAFIAIAB4AG0AbABuAHMAPQAiAGgAdAB0AHAAOgAvAC8AcwBjAGgAZQBtAGEAcwAuAG0AaQBjAHIAbwBzAG8AZgB0AC4AYwBvAG0ALwBEAFIATQAvADIAMAAwADcALwAwADMALwBQAGwAYQB5AFIAZQBhAGQAeQBIAGUAYQBkAGUAcgAiACAAdgBlAHIAcwBpAG8AbgA9ACIANAAuADAALgAwAC4AMAAiAD4APABEAEEAVABBAD4APABQAFIATwBUAEUAQwBUAEkATgBGAE8APgA8AEsARQBZAEwARQBOAD4AMQA2ADwALwBLAEUAWQBMAEUATgA+ADwAQQBMAEcASQBEAD4AQQBFAFMAQwBUAFIAPAAvAEEATABHAEkARAA+ADwALwBQAFIATwBUAEUAQwBUAEkATgBGAE8APgA8AEsASQBEAD4ASgBRAHoASQArAEEAOQBwAE4AawBlAEIATQBrAE0ATwBYAEcAbQBVAHoAZwA9AD0APAAvAEsASQBEAD4APAAvAEQAQQBUAEEAPgA8AC8AVwBSAE0ASABFAEEARABFAFIAPgA=</pro>"
			});
			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("94ce86fb-07ff-4f43-adB8-93d2fa968ca2"),
				KeyId = contentKeyWithKnownId.Id,
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "#EXT-X-SESSION-KEY:METHOD=SAMPLE-AES,URI=\"skd://f8c80c25-690f-4736-8132-430e5c6994ce:51BB4F1A7E2E835B2993884BD09ADB19\",KEYFORMAT=\"com.apple.streamingkeydelivery\",KEYFORMATVERSIONS=\"1\"",
					VariantPlaylistData = "#EXT-X-KEY:METHOD=SAMPLE-AES,URI=\"skd://f8c80c25-690f-4736-8132-430e5c6994ce:51BB4F1A7E2E835B2993884BD09ADB19\",KEYFORMAT=\"com.apple.streamingkeydelivery\",KEYFORMATVERSIONS=\"1\""
				}
			});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate2WithPublicKey));

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.First().Id,

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
