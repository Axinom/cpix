using System;
using System.Collections.Generic;

namespace Axinom.Cpix
{
	/// <summary>
	/// Provides the input data used for matching key assignment rules to a certain type of sample.
	/// </summary>
	public sealed class SampleDescription
	{
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
		/// Video nominal frames per second. Only valid if Type == Video.
		/// If null, sample will not match any filters that require a specific value;
		/// </summary>
		public long? VideoFramesPerSecond { get; set; }

		/// <summary>
		/// Whether the picture uses WCG. Only valid if Type == Video.
		/// If null, sample will not match any filters that require a specific value;
		/// </summary>
		public bool? WideColorGamut { get; set; }

		/// <summary>
		/// Whether the picture uses HDR. Only valid if Type == Video.
		/// If null, sample will not match any filters that require a specific value;
		/// </summary>
		public bool? HighDynamicRange { get; set; }

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

			if (VideoFramesPerSecond != null && Type != SampleType.Video)
				throw new ArgumentException("Sample description cannot define VideoFramesPerSecond unless Type == Video.");

			if (WideColorGamut != null && Type != SampleType.Video)
				throw new ArgumentException("Sample description cannot define WideColorGamut unless Type == Video.");

			if (HighDynamicRange != null && Type != SampleType.Video)
				throw new ArgumentException("Sample description cannot define HighDynamicRange unless Type == Video.");
		}
	}
}
