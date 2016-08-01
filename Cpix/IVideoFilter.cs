namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a video filter attached to a content key assignment rule.
	/// Only video samples can match this filter - any other type of sample is never a match.
	/// </summary>
	public interface IVideoFilter
	{
		/// <summary>
		/// The minimum number of pixels that must be present in the pictures encoded by the
		/// stream containing matching samples (inclusive).
		/// 
		/// This is counted in encoded picture pixels, without considering sample aspect ratio.
		/// 
		/// If null, the minimum required pixel count is 0.
		/// </summary>
		long? MinPixels { get; }

		/// <summary>
		/// The maximum number of pixels that must be present in the pictures encoded by the
		/// stream containing matching samples (inclusive).
		/// 
		/// This is counted in encoded picture pixels, without considering sample aspect ratio.
		/// 
		/// If null, the maximum required pixel count is infinity.
		/// </summary>
		long? MaxPixels { get; }

		/// <summary>
		/// Whether the filter matches HDR or non-HDR samples or both.
		/// 
		/// If null, HDR-ness of samples is ignored.
		/// </summary>
		bool? HighDynamicRange { get; }

		/// <summary>
		/// Whether the filter matches WCG or non-WCG samples or both.
		/// 
		/// If null, WCG-ness of samples is ignored.
		/// </summary>
		bool? WideColorGamut { get; }

		/// <summary>
		/// The minimum (exclusive) framerate of the video stream whose samples match this filter.
		/// 
		/// If null, the minimum framerate is 0.
		/// </summary>
		long? MinFramesPerSecond { get; }

		/// <summary>
		/// The maximum (inclusive) framerate of the video stream whose samples match this filter.
		/// 
		/// If null, the maximum framerate is infinity.
		/// </summary>
		long? MaxFramesPerSecond { get; }
	}
}
