namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a crypto period filter attached to a content key assignment rule.
	/// </summary>
	public interface ICryptoPeriodFilter
	{
		/// <summary>
		/// The crypto period index of samples must match this value to satisfy this filter.
		/// </summary>
		long PeriodIndex { get; }
	}
}
