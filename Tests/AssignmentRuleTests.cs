using Axinom.Cpix;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class UsageRuleTests
	{
		[Fact]
		public void AddUsageRule_WithNewDocument_AddsUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRules.Count);
			Assert.Equal(contentKey.Id, document.UsageRules.Single().KeyId);
		}

		[Fact]
		public void AddUsageRule_WithLoadedDocumentWithNoExistingRules_AddsUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			document = TestHelpers.Reload(document);

			TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRules.Count);
			Assert.Equal(contentKey.Id, document.UsageRules.Single().KeyId);
		}

		[Fact]
		public void AddUsageRule_WithLoadedDocumentWithExistingRules_AddsUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			document = TestHelpers.Reload(document);

			TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.UsageRules.Count);
			Assert.Equal(contentKey.Id, document.UsageRules.First().KeyId);
			Assert.Equal(contentKey.Id, document.UsageRules.Last().KeyId);
		}

		[Fact]
		public void AddUsageRule_WithVariousInvalidRules_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule()));
			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = Guid.NewGuid()
			}));

			var contentKey = TestHelpers.GenerateContentKey();
			document.AddContentKey(contentKey);

			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = contentKey.Id,
				AudioFilter = new AudioFilter
				{
					MaxChannels = 5,
					MinChannels = 6
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = contentKey.Id,
				AudioFilter = new AudioFilter(),
				VideoFilter = new VideoFilter()
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = contentKey.Id,
				VideoFilter = new VideoFilter
				{
					MaxPixels = 10,
					MinPixels = 11
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = contentKey.Id,
				BitrateFilter = new BitrateFilter
				{
					MaxBitrate = 515145151515,
					MinBitrate = 515145151516
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddUsageRule(new UsageRule
			{
				KeyId = contentKey.Id,
				LabelFilter = new LabelFilter
				{
					Label = null
				}
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedUsageRule_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			var rule = TestHelpers.AddUsageRule(document);

			// It was validated by Add but now we corrupt it again!
			rule.KeyId = Guid.NewGuid();

			Assert.ThrowsAny<Exception>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddUsageRule_WithExistingSignature_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}

		[Fact]
		public void AddUsageRule_AfterRemovingExistingSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveUsageRuleSignatures();
			TestHelpers.AddUsageRule(document);
		}
	}
}
