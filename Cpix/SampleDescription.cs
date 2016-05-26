﻿using System;
using System.Collections.Generic;

namespace Axinom.Cpix
{
	/// <summary>
	/// Provides the input data used for matching key assignment rules to a certain type of sample.
	/// </summary>
	public sealed class SampleDescription
	{
		/// <summary>
		/// Timestamp of the sample.
		/// If null, sample will not match any time filters.
		/// </summary>
		public DateTimeOffset? Timestamp { get; set; }

		/// <summary>
		/// Type of the sample.
		/// If null, sample will not match any filters that require a specific type of sample.
		/// </summary>
		public SampleType? Type { get; set; }

		/// <summary>
		/// Nominal bitrate of the sample.
		/// If null, sample will not match any filters that require a specific bitrate.
		/// </summary>
		public long? Bitrate { get; set; }

		/// <summary>
		/// Number of audio channels in the sample. Only valid if Type == Audio.
		/// If null, sample will not match any filters that require a specific audio channel count.
		/// </summary>
		public int? AudioChannelCount { get; set; }

		/// <summary>
		/// Number of pixels in the encoded pictures. Only valid if Type == Video.
		/// If null, sample will not match any filters that require a specific picture pixel count.
		/// </summary>
		public long? PicturePixelCount { get; set; }

		/// <summary>
		/// Crypto period index of the sample.
		/// If null, sample will not match any filters that require a specific crypto period index.
		/// </summary>
		public long? CryptoPeriodIndex { get; set; }

		/// <summary>
		/// Labels that exist on the sample.
		/// If null, sample will not match any filters that require a specific label on the sample.
		/// </summary>
		public IReadOnlyCollection<string> Labels { get; set; }

		/// <summary>
		/// Validates the sample description before use.
		/// </summary>
		internal void Validate()
		{
			if (AudioChannelCount != null && Type != SampleType.Audio)
				throw new ArgumentException("Sample description cannot define audio channel count unless Type == Audio.");

			if (PicturePixelCount != null && Type != SampleType.Video)
				throw new ArgumentException("Sample description cannot define picture pixel count unless Type == Video.");
		}
	}
}