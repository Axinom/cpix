namespace Axinom.Cpix
{
	/// <summary>
	/// A key period filter attached to a new content key assignment rule.
	/// </summary>
	public sealed class KeyPeriodFilter
	{
		/// <summary>
		/// Gets or sets the ID of the content key period that is associated with
		/// this filter. Null is invalid.
		/// </summary>
		public string PeriodId { get; set; }

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (PeriodId == null)
				throw new InvalidCpixDataException("A key period filter that does not reference a content key period is invalid.");
		}
	}
}