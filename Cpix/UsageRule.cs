using System;
using System.Collections.Generic;
using System.Linq;

namespace Axinom.Cpix
{
	/// <summary>
	/// A content key assignment rule to be added to a CPIX document
	/// </summary>
	/// <remarks>
	/// When the rule is used to map a sample to a content key filters are combined using "AND" logic.
	/// </remarks>
	public sealed class UsageRule : IUsageRule
	{
		/// <summary>
		/// The content key referenced by this assignment rule.
		/// </summary>
		public Guid KeyId { get; set; }

		/// <summary>
		/// Label filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		public LabelFilter LabelFilter { get; set; }

		/// <summary>
		/// Video filter to apply when mapping samples.
		/// If non-null, only video samples are valid candidates to match this filter.
		/// If null, all samples are a match for this filter.
		/// </summary>
		public VideoFilter VideoFilter { get; set; }

		/// <summary>
		/// Audio filter to apply when mapping samples.
		/// If non-null, only audio samples are valid candidates to match this filter.
		/// If null, all samples are a match for this filter.
		/// </summary>
		public AudioFilter AudioFilter { get; set; }

		/// <summary>
		/// Bitrate filter to apply when mapping samples.
		/// If null, all samples are a match for this filter.
		/// </summary>
		public BitrateFilter BitrateFilter { get; set; }

		ILabelFilter IUsageRule.LabelFilter => LabelFilter;
		IVideoFilter IUsageRule.VideoFilter => VideoFilter;
		IAudioFilter IUsageRule.AudioFilter => AudioFilter;
		IBitrateFilter IUsageRule.BitrateFilter => BitrateFilter;

		/// <summary>
		/// Validates the data in the object before it is accepted for use by this library.
		/// </summary>
		internal void Validate(IReadOnlyCollection<IContentKey> contentKeys)
		{
			if (!contentKeys.Any(ck => ck.Id == KeyId))
				throw new InvalidCpixDataException("Content key assignment rule references a content key that is not defined in the CPIX document.");

			if (VideoFilter != null && AudioFilter != null)
				throw new InvalidCpixDataException("Content key assignment rule contains both a video filter and an audio filter - this is an invalid combination.");

			LabelFilter?.Validate();
			VideoFilter?.Validate();
			AudioFilter?.Validate();
			BitrateFilter?.Validate();
		}
	}
}
