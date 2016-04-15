namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a label filter attached to a content key assignment rule.
	/// </summary>
	public interface ILabelFilter
	{
		/// <summary>
		/// The label that must exist on samples that match this filter. The meaning of labels is implementation-defined.
		/// 
		/// A null value is a syntax error.
		/// </summary>
		string Label { get; }
	}
}
