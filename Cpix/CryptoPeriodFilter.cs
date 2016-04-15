namespace Axinom.Cpix
{
	/// <summary>
	/// A a crypto period filter attached to a new content key assignment rule.
	/// </summary>
	public sealed class CryptoPeriodFilter : ICryptoPeriodFilter
	{
		/// <summary>
		/// The crypto period index of samples must match this value to satisfy this filter.
		/// </summary>
		public long PeriodIndex { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			// Can't go wrong with this one. Anything is valid. Even negative indexes because why not.
		}
	}
}
