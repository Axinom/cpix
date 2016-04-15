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
		/// If null, the minimum required pixel count is 0.
		/// </summary>
		long? MinPixels { get; }

		/// <summary>
		/// The maximum number of pixels that must be present in the pictures encoded by the
		/// stream containing matching samples (inclusive).
		/// 
		/// If null, the maximum required pixel count is infinity.
		/// </summary>
		long? MaxPixels { get; }
	}
}
