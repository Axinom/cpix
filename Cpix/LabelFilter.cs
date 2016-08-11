namespace Axinom.Cpix
{
	/// <summary>
	/// A label filter attached to a new content key assignment rule.
	/// </summary>
	public sealed class LabelFilter
	{
		/// <summary>
		/// The label that must exist on a content key context that matches this filter.
		/// The meaning of labels is implementation-defined - they are just arbitrary freeform strings.
		/// 
		/// A null value is a syntax error.
		/// </summary>
		public string Label { get; set; }

		public LabelFilter()
		{
		}

		public LabelFilter(string label)
		{
			Label = label;
		}

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate()
		{
			if (Label == null)
				throw new InvalidCpixDataException("A label filter that does not reference a label is invalid.");
		}
	}
}
