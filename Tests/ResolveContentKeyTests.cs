using Axinom.Cpix;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
	// TODO: Test for inclusivity and exclusivity of numeric ranges.

	public sealed class ResolveContentKeyTests
	{
		[Fact]
		public void ResolveContentKey_WithNoRules_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			Assert.Throws<ContentKeyResolveImpossibleException>(() => document.ResolveContentKey(new ContentKeyContext()));
		}

		[Fact]
		public void ResolveContentKey_WithMultipleMatches_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			// Two rules that match all contexts - resolving a key will never work with this document.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id
			});

			Assert.Throws<ContentKeyResolveAmbiguityException>(() => document.ResolveContentKey(new ContentKeyContext()));
		}

		[Fact]
		public void ResolveContentKey_WithUnsupportedFilters_Fails()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			// This matches all contexts.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id
			});

			// This matches some crazy label AND contains unsupported filters.
			// The presence of this should be enough to completely disable any key resolving.
			var rule = new UsageRule
			{
				KeyId = key.Id,

				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "299999999999999999999999999999999999"
					}
				}
			};
			document.UsageRules.Add(rule);

			// We do this after Add() since normally such filters cannot be added (and save would also reject it).
			rule.ContainsUnsupportedFilters = true;

			Assert.Throws<NotSupportedException>(() => document.ResolveContentKey(new ContentKeyContext()));
		}

		[Fact]
		public void ResolveContentKey_WithRulesButNoMatch_ThrowsException()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,
				AudioFilters = new[]
				{
					new AudioFilter()
				}
			});

			// Not finding a match is an error - all contexts must be matched to exactly one content key.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video
			}));
		}

		[Fact]
		public void ResolveContentKey_WithSingleMatch_Succeeds()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,
				AudioFilters = new[]
				{
					new AudioFilter()
				}
			});

			Assert.Equal(key, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio
			}));
		}

		[Fact]
		public void ResolveContentKey_WithOneFilterTypeMatchOtherFilterTypeNonMatch_Fails()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,

				// This rule requries the context to be both audio and video. Nothing should ever match it!
				AudioFilters = new[]
				{
					new AudioFilter()
				},
				VideoFilters = new[]
				{
					new VideoFilter()
				}
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video
			}));
		}

		[Fact]
		public void ResolveContentKey_WithTwoBitrateRanges_MatchesInEitherRangeButNotOutside()
		{
			var key = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key.Id,

				// Either bitrate 0-100 or bitrate 1000-1100. Both ranges should match but not in between.
				BitrateFilters = new[]
				{
					new BitrateFilter
					{
						MinBitrate = 0,
						MaxBitrate = 100
					},
					new BitrateFilter
					{
						MinBitrate = 1000,
						MaxBitrate = 1100
					}
				}
			});

			document.ResolveContentKey(new ContentKeyContext
			{
				Bitrate = 50
			});
			document.ResolveContentKey(new ContentKeyContext
			{
				Bitrate = 1050
			});

			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Bitrate = 500
			}));
		}

		[Fact]
		public void ResolveContentKey_WithRealisticFilters_MatchesAsExpected()
		{
			var lowValuePeriod1 = TestHelpers.GenerateContentKey();
			var mediumValuePeriod1 = TestHelpers.GenerateContentKey();
			var highValuePeriod1 = TestHelpers.GenerateContentKey();
			var lowValueAudio = TestHelpers.GenerateContentKey();
			var highValueAudio = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(lowValuePeriod1);
			document.ContentKeys.Add(mediumValuePeriod1);
			document.ContentKeys.Add(highValuePeriod1);
			document.ContentKeys.Add(lowValueAudio);
			document.ContentKeys.Add(highValueAudio);

			const int mediumValuePixelCount = 1280 * 720;
			const int highValuePixelCount = 1920 * 1080;

			document.UsageRules.Add(new UsageRule
			{
				KeyId = lowValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MaxPixels = mediumValuePixelCount - 1
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = mediumValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MinPixels = mediumValuePixelCount,
						MaxPixels = highValuePixelCount - 1
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = highValuePeriod1.Id,
				VideoFilters = new[]
				{
					new VideoFilter
					{
						MinPixels = highValuePixelCount
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = lowValueAudio.Id,
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MaxChannels = 2
					}
				}
			});

			document.UsageRules.Add(new UsageRule
			{
				KeyId = highValueAudio.Id,
				AudioFilters = new[]
				{
					new AudioFilter
					{
						MinChannels = 3
					}
				}
			});

			// Audio with unknown channel count should not match either rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio
			}));

			// If we do have a channel count, however, it should match the appropriate rule.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				AudioChannelCount = 1
			}));

			// Audio rules have no time range, so it should match even outside the defined periods.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				AudioChannelCount = 1
			}));

			// Or, indeed, even without any known timestamp.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				AudioChannelCount = 1
			}));

			// There is a rollover to a new key at 3 channels.
			Assert.Equal(lowValueAudio, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				AudioChannelCount = 2
			}));
			Assert.Equal(highValueAudio, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				AudioChannelCount = 3
			}));

			// Video without resolution should not match any rule.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video
			}));

			// Extra data should not change anything in matching.
			Assert.Equal(highValuePeriod1, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,
				PicturePixelCount = highValuePixelCount * 2,
				Bitrate = 254726,
			}));
		}

		[Fact]
		public void ResolveContentKey_WithLabels_MatchesWithAnyMatchingLabel()
		{
			var key1 = TestHelpers.GenerateContentKey();
			var key2 = TestHelpers.GenerateContentKey();
			var key3 = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(key1);
			document.ContentKeys.Add(key2);
			document.ContentKeys.Add(key3);

			document.UsageRules.Add(new UsageRule
			{
				KeyId = key1.Id,
				AudioFilters = new[]
				{
					new AudioFilter(),
				},
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label1"
					}
				}
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key2.Id,
				VideoFilters = new[]
				{
					new VideoFilter(),
				},
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label2"
					}
				}
			});
			document.UsageRules.Add(new UsageRule
			{
				KeyId = key3.Id,
				LabelFilters = new[]
				{
					new LabelFilter
					{
						Label = "label3"
					}
				}
			});

			Assert.Equal(key1, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			Assert.Equal(key1, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				Labels = new[]
				{
					"label1"
				}
			}));

			// Unrelated labels should not be an obstacle to matching.
			Assert.Equal(key1, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				Labels = new[]
				{
					"label1",
					"xxxyyyzz"
				}
			}));

			Assert.Equal(key2, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,
				Labels = new[]
				{
					"label1",
					"label2",
					"someVideoLabel"
				}
			}));

			// Labels from otherwise nonmatching rules should have no effects.
			Assert.Throws<ContentKeyResolveException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,
				Labels = new[]
				{
					"label1",
				}
			}));

			// Multiple matches are evil and should not be accepted.
			Assert.Throws<ContentKeyResolveAmbiguityException>(() => document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,
				Labels = new[]
				{
					"label2",
					"label3",
				}
			}));

			// A match is a match regardless of other parameters.
			Assert.Equal(key3, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Audio,
				Labels = new[]
				{
					"label3"
				},
				AudioChannelCount = 3,
				Bitrate = 63576
			}));

			Assert.Equal(key3, document.ResolveContentKey(new ContentKeyContext
			{
				Type = ContentKeyContextType.Video,
				Labels = new[]
				{
					"label3"
				},
				Bitrate = 123,
				PicturePixelCount = 46733,
			}));
		}
	}
}
