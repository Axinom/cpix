using Axinom.Cpix;
using System.IO;
using System.Linq;
using Tests;

namespace TestVectorGenerator
{
	sealed class UsageRulesBasedOnLabels : ITestVector
	{
		public string Description => "Usage rules that map content keys using sets of labels.";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

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
