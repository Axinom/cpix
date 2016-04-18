using Axinom.Cpix;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class AssignmentRuleTests
	{
		[Fact]
		public void AddAssignmentRule_WithNewDocument_AddsAssignmentRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			TestHelpers.AddAssignmentRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.AssignmentRules.Count);
			Assert.Equal(contentKey.Id, document.AssignmentRules.Single().KeyId);
		}

		[Fact]
		public void AddAssignmentRule_WithLoadedDocumentWithNoExistingRules_AddsAssignmentRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			document = TestHelpers.Reload(document);

			TestHelpers.AddAssignmentRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.AssignmentRules.Count);
			Assert.Equal(contentKey.Id, document.AssignmentRules.Single().KeyId);
		}

		[Fact]
		public void AddAssignmentRule_WithLoadedDocumentWithExistingRules_AddsAssignmentRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			document = TestHelpers.Reload(document);

			TestHelpers.AddAssignmentRule(document);

			document = TestHelpers.Reload(document);

			TestHelpers.AddAssignmentRule(document);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.AssignmentRules.Count);
			Assert.Equal(contentKey.Id, document.AssignmentRules.First().KeyId);
			Assert.Equal(contentKey.Id, document.AssignmentRules.Last().KeyId);
		}

		[Fact]
		public void AddAssignmentRule_WithVariousInvalidRules_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule()));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = Guid.NewGuid()
			}));

			var contentKey = TestHelpers.GenerateContentKey();
			document.AddContentKey(contentKey);

			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				AudioFilter = new AudioFilter
				{
					MaxChannels = 5,
					MinChannels = 6
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				AudioFilter = new AudioFilter(),
				VideoFilter = new VideoFilter()
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				VideoFilter = new VideoFilter
				{
					MaxPixels = 10,
					MinPixels = 11
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				BitrateFilter = new BitrateFilter
				{
					MaxBitrate = 515145151515,
					MinBitrate = 515145151516
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				LabelFilter = new LabelFilter
				{
					Label = null
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				TimeFilter = new TimeFilter
				{
					Start = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero),
					End = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero),
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = contentKey.Id,
				TimeFilter = new TimeFilter()
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedAssignmentRule_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			var rule = TestHelpers.AddAssignmentRule(document);

			// It was validated by Add but now we corrupt it again!
			rule.KeyId = Guid.NewGuid();

			Assert.ThrowsAny<Exception>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddAssignmentRule_WithExistingSignature_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddAssignmentRule(document));
		}

		[Fact]
		public void AddAssignmentRule_AfterRemovingExistingSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveAssignmentRuleSignatures();
			TestHelpers.AddAssignmentRule(document);
		}
	}
}
