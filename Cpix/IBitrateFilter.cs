namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a bitrate filter attached to a content key assignment rule.
	/// </summary>
	public interface IBitrateFilter
	{
		/// <summary>
		/// The minimum nominal bitrate of a stream containing matching samples (inclusive).
		/// 
		/// If null, the minimum bitrate is zero.
		/// </summary>
		long? MinBitrate { get; }

		/// <summary>
		/// The maximum nominal bitrate of a stream containing matching samples (inclusive).
		/// 
		/// If null, the maximum bitrate is infinity.
		/// </summary>
		long? MaxBitrate { get; }
	}
}
