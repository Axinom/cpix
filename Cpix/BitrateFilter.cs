namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a bitrate filter attached to a content key assignment rule.
	/// </summary>
	public sealed class BitrateFilter
	{
		/// <summary>
		/// The minimum nominal bitrate of a stream containing matching samples (inclusive).
		/// 
		/// If null, the minimum bitrate is zero.
		/// </summary>
		public long? MinBitrate { get; set; }

		/// <summary>
		/// The maximum nominal bitrate of a stream containing matching samples (inclusive).
		/// 
		/// If null, the maximum bitrate is infinity.
		/// </summary>
		public long? MaxBitrate { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (MinBitrate > MaxBitrate)
				throw new InvalidCpixDataException("Bitrate filter minimum bitrate cannot be greater than its maximum bitrate.");
		}
	}
}
