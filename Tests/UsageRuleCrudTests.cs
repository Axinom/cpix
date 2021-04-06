using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class UsageRuleCrudTests
	{
		[Fact]
		public void AddUsageRule_WithMinimalData_AddsExpectedUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var expectedRule = new UsageRule
			{
				KeyId = contentKey.Id
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.UsageRules.Add(expectedRule);

			document = TestHelpers.Reload(document);

			Assert.Single(document.UsageRules);

			var actualRule = document.UsageRules.First();

			Assert.False(actualRule.ContainsUnsupportedFilters);

			Assert.Equal(expectedRule.KeyId, actualRule.KeyId);
			Assert.Null(actualRule.IntendedTrackType);
			Assert.Empty(actualRule.AudioFilters);
			Assert.Empty(actualRule.BitrateFilters);
			Assert.Empty(actualRule.KeyPeriodFilters);
			Assert.Empty(actualRule.LabelFilters);
			Assert.Empty(actualRule.VideoFilters);
		}

		[Fact]
		public void AddUsageRule_WithVariousData_AddsExpectedUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });

			var expectedRule = TestHelpers.AddUsageRule(document);

			document = TestHelpers.Reload(document);

			Assert.Single(document.UsageRules);

			var actualRule = document.UsageRules.First();

			Assert.False(actualRule.ContainsUnsupportedFilters);

			Assert.Equal(expectedRule.KeyId, actualRule.KeyId);
			Assert.Equal(expectedRule.IntendedTrackType, actualRule.IntendedTrackType);
			Assert.Equal(expectedRule.AudioFilters.Count, actualRule.AudioFilters.Count);
			Assert.Equal(expectedRule.BitrateFilters.Count, actualRule.BitrateFilters.Count);
			Assert.Equal(expectedRule.KeyPeriodFilters.Count, actualRule.KeyPeriodFilters.Count);
			Assert.Equal(expectedRule.LabelFilters.Count, actualRule.LabelFilters.Count);
			Assert.Equal(expectedRule.VideoFilters.Count, actualRule.VideoFilters.Count);
		}

		[Fact]
		public void AddUsageRule_WithLoadedDocumentWithNoExistingRules_AddsUsageRule()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });

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
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });

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
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				KeyPeriodFilters = new[]
				{
					new KeyPeriodFilter
					{
						PeriodId = null
					}
				}
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.UsageRules.Add(new UsageRule
			{
				KeyId = contentKey.Id,
				KeyPeriodFilters = new[]
				{
					new KeyPeriodFilter
					{
						PeriodId = "unknownID"
					}
				}
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedUsageRule_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });

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
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });
			
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
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period1", Index = 1 });
			
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
			var contentKeyPeriod = new ContentKeyPeriod { Id = "period1", Index = 1 };

			var rule = new UsageRule
			{
				KeyId = contentKey.Id,
				IntendedTrackType = "XUHD",
				KeyPeriodFilters = new [] { new KeyPeriodFilter { PeriodId = contentKeyPeriod.Id} }
			};

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.ContentKeyPeriods.Add(contentKeyPeriod);
			document.UsageRules.Add(rule);
			document.UsageRules.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			Assert.Single(document.UsageRules);
			Assert.Equal(rule.IntendedTrackType, document.UsageRules.First().IntendedTrackType);

			Assert.Single(document.UsageRules.First().KeyPeriodFilters);
			Assert.Equal(rule.KeyPeriodFilters.First().PeriodId, document.UsageRules.First().KeyPeriodFilters.First().PeriodId);
		}

		[Theory]
		[InlineData(null, "is missing")]
		[InlineData("periodId =\"1_cannot_start_with_a_number\"", "invalid according to its datatype")]
		[InlineData("periodId =\"I_reference_an_unknown_ID\"", "Reference to undeclared ID")]
		public void Load_WithUsageRuleContainingKeyPeriodFilterWithNonSchemaCompliantPeriodId_Fails(string invalidPeriodIdAttribute, string expectedErrorMessage)
		{
			const string cpixTemplate = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\"><ContentKeyList><ContentKey kid=\"64f586c4-a57a-40cf-b603-7f4196a75219\"></ContentKey></ContentKeyList><ContentKeyPeriodList><ContentKeyPeriod id=\"period1\" index=\"1\" /></ContentKeyPeriodList><ContentKeyUsageRuleList><ContentKeyUsageRule kid=\"64f586c4-a57a-40cf-b603-7f4196a75219\"><KeyPeriodFilter {0} /></ContentKeyUsageRule></ContentKeyUsageRuleList></CPIX>";
			var cpix = string.Format(cpixTemplate, invalidPeriodIdAttribute);

			var ex = Assert.Throws<XmlSchemaValidationException>(() => CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(cpix))));
			Assert.Contains(expectedErrorMessage, ex.Message);
		}
	}
}
