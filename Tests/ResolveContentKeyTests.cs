using Axinom.Cpix;
using System;
using Xunit;

namespace Tests
{
	public sealed class ResolveContentKeyTests
	{
		[Fact]
		public void ResolveContentKey_WithNoRules_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key);

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription()));
		}

		[Fact]
		public void ResolveContentKey_WithMultipleMatches_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key);

			// Two rules that match all samples - resolving a key will never work with this document.
			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key.Id
			});
			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key.Id
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription()));
		}

		[Fact]
		public void ResolveContentKEy_WithRulesButNoMatch_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key);

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key.Id,
				AudioFilter = new AudioFilter()
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video
			}));
		}

		[Fact]
		public void ResolveContentKey_WithSingleMatch_Succeeds()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key);

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key.Id,
				AudioFilter = new AudioFilter()
			});

			Assert.Equal(key, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio
			}));
		}

		[Fact]
		public void ResolveContentKey_WithTimeFilter_MatchesAsExpected()
		{
			var key1 = TestHelpers.GenerateContentKey();
			var key2 = TestHelpers.GenerateContentKey();
			var key3 = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key1);
			document.AddContentKey(key2);
			document.AddContentKey(key3);

			var time = new DateTimeOffset(2017, 1, 1, 1, 1, 1, TimeSpan.Zero);

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key1.Id,
				TimeFilter = new TimeFilter
				{
					// This rule does not cover the sample - ends right on the moment (exclusively).
					End = time
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key2.Id,
				TimeFilter = new TimeFilter
				{
					// This rule does covers the sample and the 1 second after that.
					Start = time,
					End = time + TimeSpan.FromSeconds(1)
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key3.Id,
				TimeFilter = new TimeFilter
				{
					// This rule does not cover the sample - it starts after the sample description.
					Start = time + TimeSpan.FromSeconds(1)
				}
			});

			Assert.Equal(key2, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = time
			}));
		}

		[Fact]
		public void ResolveContentKey_WithRealisticFilters_MatchesAsExpected()
		{
			var lowValuePeriod1 = TestHelpers.GenerateContentKey();
			var lowValuePeriod2 = TestHelpers.GenerateContentKey();
			var mediumValuePeriod1 = TestHelpers.GenerateContentKey();
			var mediumValuePeriod2 = TestHelpers.GenerateContentKey();
			var highValuePeriod1 = TestHelpers.GenerateContentKey();
			var highValuePeriod2 = TestHelpers.GenerateContentKey();
			var lowValueAudio = TestHelpers.GenerateContentKey();
			var highValueAudio = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(lowValuePeriod1);
			document.AddContentKey(lowValuePeriod2);
			document.AddContentKey(mediumValuePeriod1);
			document.AddContentKey(mediumValuePeriod2);
			document.AddContentKey(highValuePeriod1);
			document.AddContentKey(highValuePeriod2);
			document.AddContentKey(lowValueAudio);
			document.AddContentKey(highValueAudio);

			var periodDuration = TimeSpan.FromMinutes(10);
			var period1Start = new DateTimeOffset(2017, 1, 1, 1, 1, 1, TimeSpan.Zero);
			var period2Start = period1Start + periodDuration;

			const int mediumValuePixelCount = 1280 * 720;
			const int highValuePixelCount = 1920 * 1080;

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = lowValuePeriod1.Id,
				TimeFilter = new TimeFilter
				{
					Start = period1Start,
					End = period1Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MaxPixels = mediumValuePixelCount - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = mediumValuePeriod1.Id,
				TimeFilter = new TimeFilter
				{
					Start = period1Start,
					End = period1Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = mediumValuePixelCount,
					MaxPixels = highValuePixelCount - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = highValuePeriod1.Id,
				TimeFilter = new TimeFilter
				{
					Start = period1Start,
					End = period1Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = highValuePixelCount
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = lowValuePeriod2.Id,
				TimeFilter = new TimeFilter
				{
					Start = period2Start,
					End = period2Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MaxPixels = mediumValuePixelCount - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = mediumValuePeriod2.Id,
				TimeFilter = new TimeFilter
				{
					Start = period2Start,
					End = period2Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = mediumValuePixelCount,
					MaxPixels = highValuePixelCount - 1
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = highValuePeriod2.Id,
				TimeFilter = new TimeFilter
				{
					Start = period2Start,
					End = period2Start + periodDuration
				},
				VideoFilter = new VideoFilter
				{
					MinPixels = highValuePixelCount
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = lowValueAudio.Id,
				AudioFilter = new AudioFilter
				{
					MaxChannels = 2
				}
			});

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = highValueAudio.Id,
				AudioFilter = new AudioFilter
				{
					MinChannels = 3
				}
			});

			// Audio with unknown channel count should not match either rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Audio
			}));

			// If we do have a channel count, however, it should match the appropriate rule.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// Audio rules have no time range, so it should match even outside the defined periods.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start - TimeSpan.FromHours(1),
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// Or, indeed, even without any known timestamp.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				AudioChannelCount = 1
			}));

			// There is a rollover to a new key at 3 channels.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Audio,
				AudioChannelCount = 2
			}));
			Assert.Equal(highValueAudio, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Audio,
				AudioChannelCount = 3
			}));

			// Video samples without resolution should not match any rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Video
			}));

			// Video samples before defined time range should not match any rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start - TimeSpan.FromHours(1),
				Type = SampleType.Video,
				PicturePixelCount = 123456
			}));

			// Video samples before defined time range should not match any rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period2Start + TimeSpan.FromHours(1),
				Type = SampleType.Video,
				PicturePixelCount = 123456
			}));

			// Video samples within defined time range should match expected rules.
			Assert.Equal(lowValuePeriod1, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Video,
				PicturePixelCount = 123
			}));
			Assert.Equal(lowValuePeriod2, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period2Start,
				Type = SampleType.Video,
				PicturePixelCount = 123
			}));
			Assert.Equal(mediumValuePeriod1, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Video,
				PicturePixelCount = mediumValuePixelCount
			}));
			Assert.Equal(mediumValuePeriod2, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period2Start,
				Type = SampleType.Video,
				PicturePixelCount = mediumValuePixelCount
			}));
			Assert.Equal(highValuePeriod1, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period1Start,
				Type = SampleType.Video,
				PicturePixelCount = highValuePixelCount
			}));
			Assert.Equal(highValuePeriod2, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period2Start,
				Type = SampleType.Video,
				PicturePixelCount = highValuePixelCount
			}));

			// Extra data should not change anything in matching.
			Assert.Equal(highValuePeriod2, document.ResolveContentKey(new SampleDescription
			{
				Timestamp = period2Start + TimeSpan.FromSeconds(5),
				Type = SampleType.Video,
				PicturePixelCount = highValuePixelCount * 2,
				Bitrate = 254726,
				CryptoPeriodIndex = 3
			}));
		}

		[Fact]
		public void ResolveContentKey_WithLabeledSamples_MatchesWithAnyMatchingLabel()
		{
			var key1 = TestHelpers.GenerateContentKey();
			var key2 = TestHelpers.GenerateContentKey();
			var key3 = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(key1);
			document.AddContentKey(key2);
			document.AddContentKey(key3);

			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key1.Id,
				AudioFilter = new AudioFilter(),
				LabelFilter = new LabelFilter
				{
					Label = "label1"
				}
			});
			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key2.Id,
				VideoFilter = new VideoFilter(),
				LabelFilter = new LabelFilter
				{
					Label = "label2"
				}
			});
			document.AddAssignmentRule(new AssignmentRule
			{
				KeyId = key3.Id,
				LabelFilter = new LabelFilter
				{
					Label = "label3"
				}
			});

			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			// Unrelated labels should not be an obstacle to matching.
			Assert.Equal(key1, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label1",
					"xxxyyyzz"
				}
			}));

			Assert.Equal(key2, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label1",
					"label2",
					"someVideoLabel"
				}
			}));

			// Labels from otherwise nonmatching rules should have no effects.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label1",
				}
			}));

			// Multiple matches are evil and should not be accepted.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label2",
					"label3",
				}
			}));

			// A match is a match regardless of other parameters.
			Assert.Equal(key3, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Audio,
				Labels = new[]
				{
					"label3"
				},
				AudioChannelCount = 3,
				Bitrate = 63576
			}));

			Assert.Equal(key3, document.ResolveContentKey(new SampleDescription
			{
				Type = SampleType.Video,
				Labels = new[]
				{
					"label3"
				},
				Bitrate = 123,
				PicturePixelCount = 46733,
				Timestamp = new DateTimeOffset(2001, 1, 1, 1, 1, 1, TimeSpan.Zero)
			}));
		}
	}
}
