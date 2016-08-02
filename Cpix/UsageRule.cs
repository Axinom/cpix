using System;
using System.Collections.Generic;

namespace Axinom.Cpix
{
	public sealed class UsageRule : Entity, IUsageRule
	{
		/// <summary>
		/// The ID of the content key that this usage rule applies to.
		/// </summary>
		public Guid KeyId { get; set; }

		public ICollection<VideoFilter> VideoFilters { get; set; } = new List<VideoFilter>();
		public ICollection<AudioFilter> AudioFilters { get; set; } = new List<AudioFilter>();
		public ICollection<LabelFilter> LabelFilters { get; set; } = new List<LabelFilter>();
		public ICollection<BitrateFilter> BitrateFilters { get; set; } = new List<BitrateFilter>();

		IReadOnlyCollection<IVideoFilter> IUsageRule.VideoFilters => (IReadOnlyCollection<IVideoFilter>)VideoFilters;
		IReadOnlyCollection<IAudioFilter> IUsageRule.AudioFilters => (IReadOnlyCollection<IAudioFilter>)AudioFilters;
		IReadOnlyCollection<ILabelFilter> IUsageRule.LabelFilters => (IReadOnlyCollection<ILabelFilter>)LabelFilters;
		IReadOnlyCollection<IBitrateFilter> IUsageRule.BitrateFilters => (IReadOnlyCollection<IBitrateFilter>)BitrateFilters;

		internal override void ValidateNewEntity()
		{
			throw new NotImplementedException();
		}

		internal override void ValidateExistingEntity()
		{
			throw new NotImplementedException();
		}
	}
}
