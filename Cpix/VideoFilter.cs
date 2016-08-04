namespace Axinom.Cpix
{
	/// <summary>
	/// A video filter attached to a new content key assignment rule.
	/// Only video samples can match this filter - any other type of sample is never a match.
	/// </summary>
	public sealed class VideoFilter
	{
		/// <summary>
		/// The minimum number of pixels that must be present in the pictures encoded by the
		/// stream containing matching samples (inclusive).
		/// 
		/// If null, the minimum required pixel count is 0.
		/// </summary>
		public long? MinPixels { get; set; }

		/// <summary>
		/// The maximum number of pixels that must be present in the pictures encoded by the
		/// stream containing matching samples (inclusive).
		/// 
		/// If null, the maximum required pixel count is infinity.
		/// </summary>
		public long? MaxPixels { get; set; }

		/// <summary>
		/// Whether the filter matches HDR or non-HDR samples or both.
		/// 
		/// If null, HDR-ness of samples is ignored.
		/// </summary>
		public bool? HighDynamicRange { get; set; }

		/// <summary>
		/// Whether the filter matches WCG or non-WCG samples or both.
		/// 
		/// If null, WCG-ness of samples is ignored.
		/// </summary>
		public bool? WideColorGamut { get; set; }

		/// <summary>
		/// The minimum (exclusive) framerate of the video stream whose samples match this filter.
		/// 
		/// If null, the minimum framerate is 0.
		/// </summary>
		public long? MinFramesPerSecond { get; set; }

		/// <summary>
		/// The maximum (inclusive) framerate of the video stream whose samples match this filter.
		/// 
		/// If null, the maximum framerate is infinity.
		/// </summary>
		public long? MaxFramesPerSecond { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (MinPixels > MaxPixels)
				throw new InvalidCpixDataException("Video filter minimum pixel count cannot be greater than its maximum pixel count.");

			if (MinFramesPerSecond > MaxFramesPerSecond)
				throw new InvalidCpixDataException("Video filter minimum framerate cannot be greater than its maximum framerate.");
		}
	}
}
