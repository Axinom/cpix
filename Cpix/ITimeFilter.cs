using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a time filter attached to a content key assignment rule.
	/// </summary>
	public interface ITimeFilter
	{
		/// <summary>
		/// Start timestamp of the matching time period (inclusive).
		/// If null, start time of matching range is unbounded.
		/// </summary>
		DateTimeOffset? Start { get; }

		/// <summary>
		/// Start timestamp of the matching time period (exclusive).
		/// If null, end time of matching range is unbounded.
		/// </summary>
		DateTimeOffset? End { get; }
	}
}
