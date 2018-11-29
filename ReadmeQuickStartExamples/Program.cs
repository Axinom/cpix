using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Axinom.Cpix.ReadmeQuickStartExamples
{
	class Program
	{
		static void Main(string[] args)
		{
			// Here we hold the code for the quick start examples in the readme file, to validate that the code works.

			Console.WriteLine("EXAMPLE: Writing CPIX");
			WritingCpixExample();

			Console.WriteLine("EXAMPLE: Reading CPIX");
			ReadingCpixExample();

			Console.WriteLine("EXAMPLE: Modifying CPIX");
			ModifyingCpixExample();

			Console.WriteLine("EXAMPLE: Mapping content keys.");
			MappingContentKeysExample();
		}

		private static void WritingCpixExample()
		{
			var document = new CpixDocument();
			// Let's create a CPIX document with two content keys.

			document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.Parse("f8c80c25-690f-4736-8132-430e5c6994ce"),
				Value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6 }
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[] { 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 1, 2, 3, 4, 5 }
			});

			// Let's also add Widevine, PlayReady and FairPlay signaling data and
			// associate it with the first content key.

			// Widevine.
			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("edef8ba9-79d6-4ace-a3c8-27dcd51d21ed"),
				KeyId = document.ContentKeys.First().Id,
				ContentProtectionData = "<cenc:pssh xmlns:cenc=\"urn:mpeg:cenc:2013\">AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==</cenc:pssh>",
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "#EXT-X-SESSION-KEY:METHOD=SAMPLE-AES,URI=\"data:text/plain;base64,AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==\",KEYID=0xF8C80C25690F47368132430E5C6994CE,KEYFORMAT=\"urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\",KEYFORMATVERSIONS=\"1\"",
					VariantPlaylistData = "#EXT-X-KEY:METHOD=SAMPLE-AES,URI=\"data:text/plain;base64,AAAANHBzc2gAAAAA7e+LqXnWSs6jyCfc1R0h7QAAABQIARIQ+MgMJWkPRzaBMkMOXGmUzg==\",KEYID=0xF8C80C25690F47368132430E5C6994CE,KEYFORMAT=\"urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\",KEYFORMATVERSIONS=\"1\""
				}
			});

			// PlayReady.
			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("9a04f079-9840-4286-ab92-e65be0885f95"),
				KeyId = document.ContentKeys.First().Id,
				ContentProtectionData =
					"<cenc:pssh xmlns:cenc=\"urn:mpeg:cenc:2013\">AAAB5HBzc2gAAAAAmgTweZhAQoarkuZb4IhflQAAAcTEAQAAAQABALoBPABXAFIATQBIAEUAQQBEAEUAUgAgAHgAbQBsAG4AcwA9ACIAaAB0AHQAcAA6AC8ALwBzAGMAaABlAG0AYQBzAC4AbQBpAGMAcgBvAHMAbwBmAHQALgBjAG8AbQAvAEQAUgBNAC8AMgAwADAANwAvADAAMwAvAFAAbABhAHkAUgBlAGEAZAB5AEgAZQBhAGQAZQByACIAIAB2AGUAcgBzAGkAbwBuAD0AIgA0AC4AMAAuADAALgAwACIAPgA8AEQAQQBUAEEAPgA8AFAAUgBPAFQARQBDAFQASQBOAEYATwA+ADwASwBFAFkATABFAE4APgAxADYAPAAvAEsARQBZAEwARQBOAD4APABBAEwARwBJAEQAPgBBAEUAUwBDAFQAUgA8AC8AQQBMAEcASQBEAD4APAAvAFAAUgBPAFQARQBDAFQASQBOAEYATwA+ADwASwBJAEQAPgBKAFEAegBJACsAQQA5AHAATgBrAGUAQgBNAGsATQBPAFgARwBtAFUAegBnAD0APQA8AC8ASwBJAEQAPgA8AC8ARABBAFQAQQA+ADwALwBXAFIATQBIAEUAQQBEAEUAUgA+AA==</cenc:pssh>" +
					"<pro xmlns=\"urn:microsoft:playready\">xAEAAAEAAQC6ATwAVwBSAE0ASABFAEEARABFAFIAIAB4AG0AbABuAHMAPQAiAGgAdAB0AHAAOgAvAC8AcwBjAGgAZQBtAGEAcwAuAG0AaQBjAHIAbwBzAG8AZgB0AC4AYwBvAG0ALwBEAFIATQAvADIAMAAwADcALwAwADMALwBQAGwAYQB5AFIAZQBhAGQAeQBIAGUAYQBkAGUAcgAiACAAdgBlAHIAcwBpAG8AbgA9ACIANAAuADAALgAwAC4AMAAiAD4APABEAEEAVABBAD4APABQAFIATwBUAEUAQwBUAEkATgBGAE8APgA8AEsARQBZAEwARQBOAD4AMQA2ADwALwBLAEUAWQBMAEUATgA+ADwAQQBMAEcASQBEAD4AQQBFAFMAQwBUAFIAPAAvAEEATABHAEkARAA+ADwALwBQAFIATwBUAEUAQwBUAEkATgBGAE8APgA8AEsASQBEAD4ASgBRAHoASQArAEEAOQBwAE4AawBlAEIATQBrAE0ATwBYAEcAbQBVAHoAZwA9AD0APAAvAEsASQBEAD4APAAvAEQAQQBUAEEAPgA8AC8AVwBSAE0ASABFAEEARABFAFIAPgA=</pro>"
			});

			// FairPlay.
			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Parse("94ce86fb-07ff-4f43-adB8-93d2fa968ca2"),
				KeyId = document.ContentKeys.First().Id,
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "#EXT-X-SESSION-KEY:METHOD=SAMPLE-AES,URI=\"skd://f8c80c25-690f-4736-8132-430e5c6994ce:51BB4F1A7E2E835B2993884BD09ADB19\",KEYFORMAT=\"com.apple.streamingkeydelivery\",KEYFORMATVERSIONS=\"1\"",
					VariantPlaylistData = "#EXT-X-KEY:METHOD=SAMPLE-AES,URI=\"skd://f8c80c25-690f-4736-8132-430e5c6994ce:51BB4F1A7E2E835B2993884BD09ADB19\",KEYFORMAT=\"com.apple.streamingkeydelivery\",KEYFORMATVERSIONS=\"1\""
				}
			});

			using (var myCertificateAndPrivateKey = new X509Certificate2("Cert1.pfx", "Cert1"))
			using (var recipientCertificate = new X509Certificate2("Cert2.cer"))
			{
				// Optional: we sign the list added elements to and also the document as a whole.
				document.ContentKeys.AddSignature(myCertificateAndPrivateKey);
				document.SignedBy = myCertificateAndPrivateKey;

				// Optional: the presence of recipients will automatically mark the content keys to be encrypted on save.
				document.Recipients.Add(new Recipient(recipientCertificate));

				document.Save("cpix.xml");
			}

			document.Save("cpix.xml");
		}

		private static void ReadingCpixExample()
		{
			// A suitable input document is the one generated by the "writing CPIX" quick start example.

			CpixDocument document;

			// Optional: any private keys referenced by the certificate(s) you provide to Load() will be used for
			// decrypting any encrypted content keys. Even if you do not have a matching private key, the document
			// will still be successfully loaded but you will simply not have access to the values of the content keys.
			using (var myCertificateAndPrivateKey = new X509Certificate2("Cert2.pfx", "Cert2"))
				document = CpixDocument.Load("cpix.xml", myCertificateAndPrivateKey);

			if (document.ContentKeysAreReadable)
				Console.WriteLine("We have access to the content key values.");
			else
				Console.WriteLine("The content keys are encrypted and we do not have a delivery key.");

			var firstKey = document.ContentKeys.FirstOrDefault();
			var firstSignerOfKeys = document.ContentKeys.SignedBy.FirstOrDefault();

			if (firstKey != null)
				Console.WriteLine("First content key ID: " + firstKey.Id);
			else
				Console.WriteLine("No content keys in document.");

			if (firstSignerOfKeys != null)
				Console.WriteLine("Content keys first signed by: " + firstSignerOfKeys.SubjectName.Format(false));
			else
				Console.WriteLine("The content keys collection was not signed.");

			if (document.SignedBy != null)
				Console.WriteLine("Document signed by: " + document.SignedBy.SubjectName.Format(false));
			else
				Console.WriteLine("The document as a whole was not signed.");
		}

		private static void ModifyingCpixExample()
		{
			// Scenario: we take an input document containing some content keys and define usage rules for those keys.
			// A suitable input document is the one generated by the "writing CPIX" quick start example.

			var document = CpixDocument.Load("cpix.xml");

			if (document.ContentKeys.Count() < 2)
				throw new Exception("This example assumes at least 2 content keys to be present in the CPIX document.");

			// We are modifying the document, so we must first remove any document signature.
			document.SignedBy = null;

			// We are going to add some usage rules, so remove any signature on usage rules.
			document.UsageRules.RemoveAllSignatures();

			// If any usage rules already exist, get rid of them all.
			document.UsageRules.Clear();

			// Assign the first content key to all audio streams.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.First().Id,

				AudioFilters = new[] { new AudioFilter() }
			});

			// Assign the second content key to all video streams.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.Skip(1).First().Id,

				VideoFilters = new[] { new VideoFilter() }
			});

			// Save all changes. Note that we do not sign or re-sign anything in this example (although we could).
			document.Save("cpix.xml");
		}

		private static void MappingContentKeysExample()
		{
			// Scenario: we take a CPIX document with content keys and usage rules for audio and video.
			// Then we map these content keys to content key contexts containing audio and video that we want to encrypt.
			// A suitable input document is the one generated by the "modifying CPIX" quick start example.

			CpixDocument document;

			using (var myCertificateAndPrivateKey = new X509Certificate2("Cert2.pfx", "Cert2"))
				document = CpixDocument.Load("cpix.xml", myCertificateAndPrivateKey);

			if (!document.ContentKeysAreReadable)
				throw new Exception("The content keys were encrypted and we did not have a delivery key.");

			// Let's imagine we have stereo audio at 32 kbps.
			var audioKey = document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,

				Bitrate = 32 * 1000,
				AudioChannelCount = 2
			});

			// Let's imagine we have both SD and HD video.
			var sdVideoKey = document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,

				Bitrate = 1 * 1000 * 1000,
				PicturePixelCount = 640 * 480,
				WideColorGamut = false,
				HighDynamicRange = false,
				VideoFramesPerSecond = 30
			});

			var hdVideoKey = document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,

				Bitrate = 4 * 1000 * 1000,
				PicturePixelCount = 1920 * 1080,
				WideColorGamut = false,
				HighDynamicRange = false,
				VideoFramesPerSecond = 30
			});

			Console.WriteLine("Key to use for audio: " + audioKey.Id);
			Console.WriteLine("Key to use for SD video: " + sdVideoKey.Id);
			Console.WriteLine("Key to use for HD video: " + hdVideoKey.Id);
		}
	}
}
