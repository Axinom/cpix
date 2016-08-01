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
			document.ContentKeys.Add(contentKey);

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
			document.ContentKeys.Add(contentKey);

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
			document.ContentKeys.Add(contentKey);

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

			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule()));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = Guid.NewGuid()
			}));

			var contentKey = TestHelpers.GenerateContentKey();
			document.ContentKeys.Add(contentKey);

			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MaxChannels = 5,
						MinChannels = 6
					}
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				AudioFilters = new[]
				{
					new AudioFilter(),
				},
				VideoFilters = new[]
				{
					new VideoFilter()
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MaxPixels = 10,
						MinPixels = 11
					}
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MaxBitrate = 515145151515,
						MinBitrate = 515145151516
					}
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = null
					}
				}
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedUsageRule_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			var rule = TestHelpers.AddUsageRule(document);

			// It was validated by Add but now we corrupt it again!
			rule.KeyId = Guid.NewGuid();

			Assert.ThrowsAny<Exception>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddUsageRule_WithExistingSignature_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}

		[Fact]
		public void AddUsageRule_AfterRemovingExistingSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.UsageRules.RemoveAllSignatures();
			TestHelpers.AddUsageRule(document);
		}
	}
}
