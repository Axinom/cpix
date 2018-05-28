using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class UsageRuleCrudTests
	{
		[Fact]
		public void AddUsageRule_WithNewDocument_AddsUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);

			TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			Assert.Single(document.UsageRules);
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

			Assert.Single(document.UsageRules);
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

			// Key ID is null.
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule()));

			// Key ID does not reference existing key.
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
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MaxFramesPerSecond = 10,
						MinFramesPerSecond = 11
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

			// It will be validated here.
			var rule = TestHelpers.AddUsageRule(document);

			// Corrupt it after validation!
			rule.KeyId = Guid.NewGuid();

			// The corruption should still be caught.
			Assert.ThrowsAny<Exception>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddUsageRule_WithExistingSignature_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}

		[Fact]
		public void AddUsageRule_AfterRemovingExistingSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			document.UsageRules.RemoveAllSignatures();
			TestHelpers.AddUsageRule(document);
		}

		[Fact]
		public void RemoveUsageRule_WithNewWritableCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();
			var rule = new UsageRule
			{
				KeyId = contentKey.Id
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.UsageRules.Add(rule);
			document.UsageRules.Remove(rule);
		}

		[Fact]
		public void RemoveUsageRule_WithLoadedWritableCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();
			var rule = new UsageRule
			{
				KeyId = contentKey.Id
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.UsageRules.Add(rule);

			document = TestHelpers.Reload(document);

			document.UsageRules.Remove(document.UsageRules.Single());
		}

		[Fact]
		public void RemoveUsageRule_WithUnknownUsageRule_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();
			var rule = new UsageRule
			{
				KeyId = contentKey.Id
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.UsageRules.Remove(rule);
		}

		[Fact]
		public void RoundTrip_WithSignedCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();
			var rule = new UsageRule
			{
				KeyId = contentKey.Id
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.UsageRules.Add(rule);
			document.UsageRules.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			Assert.Single(document.UsageRules);
		}
	}
}
