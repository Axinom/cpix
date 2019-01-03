using Axinom.Cpix.Tests;
using System;
using System.IO;
using System.Linq;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class UsageRulesBasedOnLabels : ITestVector
	{
		public string Description => "Usage rules that map content keys using sets of labels.";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("ba6c62d6-4a49-4aa4-8869-ce4d2727a2b5"),
				Value = Convert.FromBase64String("sLVGDIuvogAUW+Ay0mE9ZA==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("37e3de05-9a3b-4c69-8970-63c17a95e0b7"),
				Value = Convert.FromBase64String("UvL2JdZiEX2exVMwn796Tg==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("53abdba2-f210-43cb-bc90-f18f9a890a02"),
				Value = Convert.FromBase64String("lOgzNKBnPZlGSns+WqO8zw==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("7ae8e96f-309e-42c3-a510-24023d923373"),
				Value = Convert.FromBase64String("K9uQ8+GgwrNx4keBHnI4Xw==")
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.ElementAt(0).Id,

				LabelFilters = new[]
				{
					new LabelFilter("AllAudioStreams"),
					new LabelFilter("Audio"),
					new LabelFilter("PositionalAudio"),
					new LabelFilter("Stereo"),
				}
			});

			// Intentionally add two rules for first key ID, to emphasize that there is no limit of one.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.ElementAt(0).Id,

				LabelFilters = new[]
				{
					new LabelFilter("Commentary"),
					new LabelFilter("Telephony"),
					new LabelFilter("Speech"),
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.ElementAt(1).Id,

				LabelFilters = new[]
				{
					new LabelFilter("SD-Video"),
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.ElementAt(2).Id,

				LabelFilters = new[]
				{
					new LabelFilter("HD-Video"),
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.ElementAt(3).Id,

				LabelFilters = new[]
				{
					new LabelFilter("UHD-Video"),
					new LabelFilter("3D-Video"),
					new LabelFilter("WCG-Video"),
					new LabelFilter("HDR-Video"),
				}
			});

			// Intentionally no rule for last key ID, to emphasize that not all keys need to have usage rules.

			document.Save(outputStream);
		}
	}
}
