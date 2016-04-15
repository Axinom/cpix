using System;

namespace Axinom.Cpix
{
	/// <summary>
	/// A read-only view of a content key assignment rule that is part of a CPIX document.
	/// </summary>
	/// <remarks>
	/// When the rule is used to map a sample to a content key filters are combined using "AND" logic.
	/// </remarks>
	public interface IAssignmentRule
	{
		/// <summary>
		/// The content key referenced by this assignment rule.
		/// </summary>
		Guid KeyId { get; }

		/// <summary>
		/// Time filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		ITimeFilter TimeFilter { get; }

		/// <summary>
		/// Crypto period filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		ICryptoPeriodFilter CryptoPeriodFilter { get; }

		/// <summary>
		/// Label filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		ILabelFilter LabelFilter { get; }

		/// <summary>
		/// Video filter to apply when mapping samples.
		/// If non-null, only video samples are valid candidates to match this filter.
		/// If null, all samples are a match for this filter.
		/// </summary>
		IVideoFilter VideoFilter { get; }

		/// <summary>
		/// Audio filter to apply when mapping samples.
		/// If non-null, only audio samples are valid candidates to match this filter.
		/// If null, all samples are a match for this filter.
		/// </summary>
		IAudioFilter AudioFilter { get; }

		/// <summary>
		/// Bitrate filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		IBitrateFilter BitrateFilter { get; }
	}
}
