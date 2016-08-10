namespace Axinom.Cpix
{
	/// <summary>
	/// An audio filter attached to a new content key assignment rule.
	/// Only audio can match this filter - any other type of content key context is never a match.
	/// </summary>
	public sealed class AudioFilter
	{
		/// <summary>
		/// The minimum number of channels that must be present in the content key context (inclusive).
		/// 
		/// If null, the minimum channel count is zero.
		/// </summary>
		public int? MinChannels { get; set; }

		/// <summary>
		/// The maximum number of channels that must be present in the content key context (inclusive).
		/// 
		/// If null, the maximum channel count is infinity.
		/// </summary>
		public int? MaxChannels { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (MinChannels > MaxChannels)
				throw new InvalidCpixDataException("Audio filter minimum channel count cannot be greater than its maximum channel count.");
		}
	}
}
