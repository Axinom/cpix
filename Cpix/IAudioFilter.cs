namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a single audio filter attached to a content key assignment rule.
	/// Only audio samples can match this filter - any other type of sample is never a match.
	/// </summary>
	public interface IAudioFilter
	{
		/// <summary>
		/// The minimum number of channels that must be present in the
		/// audio stream that a matching sample belongs to (inclusive).
		/// 
		/// If null, the minimum channel count is zero.
		/// </summary>
		int? MinChannels { get; }

		/// <summary>
		/// The maximum number of channels that must be present in the
		/// audio stream that a matching sample belongs to (inclusive).
		/// 
		/// If null, the maximum channel count is infinity.
		/// </summary>
		int? MaxChannels { get; }
	}
}
