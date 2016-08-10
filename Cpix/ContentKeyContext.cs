using System;
using System.Collections.Generic;

namespace Axinom.Cpix
{
	/// <summary>
	/// Defines the context key context that content keys can be resolved for, as defined by usage rules.
	/// </summary>
	public sealed class ContentKeyContext
	{
		/// <summary>
		/// Type of the data represented by the context.
		/// If null, the context will not match any filters that require a specific type of data.
		/// </summary>
		public ContentKeyContextType? Type { get; set; }

		/// <summary>
		/// Nominal bitrate.
		/// If null, the context will not match any filters that require a specific bitrate.
		/// </summary>
		public long? Bitrate { get; set; }

		/// <summary>
		/// Number of audio channels. Only valid if Type == Audio.
		/// If null, the context will not match any filters that require a specific audio channel count.
		/// </summary>
		public int? AudioChannelCount { get; set; }

		/// <summary>
		/// Number of pixels present in the encoded pictures. Only valid if Type == Video.
		/// If null, the context will not match any filters that require a specific picture pixel count.
		/// </summary>
		public long? PicturePixelCount { get; set; }

		/// <summary>
		/// Nominal video frames per second. Only valid if Type == Video.
		/// If null, the context will not match any filters that require a specific value;
		/// </summary>
		public long? VideoFramesPerSecond { get; set; }

		/// <summary>
		/// Whether the picture uses WCG. Only valid if Type == Video.
		/// If null, the context will not match any filters that require a specific value;
		/// </summary>
		public bool? WideColorGamut { get; set; }

		/// <summary>
		/// Whether the picture uses HDR. Only valid if Type == Video.
		/// If null, the context will not match any filters that require a specific value;
		/// </summary>
		public bool? HighDynamicRange { get; set; }

		/// <summary>
		/// All the labels associated with the content key context.
		/// If null, the context will not match any filters that require a specific label.
		/// </summary>
		public IReadOnlyCollection<string> Labels { get; set; }

		/// <summary>
		/// Validates the content key context before use.
		/// </summary>
		internal void Validate()
		{
			if (AudioChannelCount != null && Type != ContentKeyContextType.Audio)
				throw new ArgumentException("Content key context cannot define audio channel count unless Type == Audio.");

			if (PicturePixelCount != null && Type != ContentKeyContextType.Video)
				throw new ArgumentException("Content key context cannot define picture pixel count unless Type == Video.");

			if (VideoFramesPerSecond != null && Type != ContentKeyContextType.Video)
				throw new ArgumentException("Content key context cannot define VideoFramesPerSecond unless Type == Video.");

			if (WideColorGamut != null && Type != ContentKeyContextType.Video)
				throw new ArgumentException("Content key context cannot define WideColorGamut unless Type == Video.");

			if (HighDynamicRange != null && Type != ContentKeyContextType.Video)
				throw new ArgumentException("Content key context cannot define HighDynamicRange unless Type == Video.");
		}
	}
}
