namespace Axinom.Cpix
{
	/// <summary>
	/// A video filter attached to a new content key assignment rule.
	/// Only video samples can match this filter - any other type of sample is never a match.
	/// </summary>
	public sealed class VideoFilter : IVideoFilter
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
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (MinPixels > MaxPixels)
				throw new InvalidCpixDataException("Video filter minimum pixel count cannot be greater than its maximum pixel count.");
		}
	}
}
