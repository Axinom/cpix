using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A a time filter attached to a new content key assignment rule.
	/// </summary>
	public sealed class TimeFilter : ITimeFilter
	{
		/// <summary>
		/// Start timestamp of the matching time period (inclusive).
		/// If null, start time of matching range is unbounded.
		/// </summary>
		public DateTimeOffset? Start { get; set; }

		/// <summary>
		/// Start timestamp of the matching time period (exclusive).
		/// If null, end time of matching range is unbounded.
		/// </summary>
		public DateTimeOffset? End { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (Start >= End)
				throw new InvalidCpixDataException("The time filter start time must be earlier than the end time if both are specified.");

			if (Start == null && End == null)
				throw new InvalidCpixDataException("The time filter must specify at least one of: start time, end time.");
		}
	}
}
