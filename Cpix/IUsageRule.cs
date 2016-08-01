using System;
using System.Collections.Generic;

namespace Axinom.Cpix
{
	public interface IUsageRule
	{
		/// <summary>
		/// The ID of the content key that this usage rule applies to.
		/// </summary>
		Guid KeyId { get; }

		IReadOnlyCollection<IVideoFilter> VideoFilters { get; }
		IReadOnlyCollection<IAudioFilter> AudioFilters { get; }
		IReadOnlyCollection<ILabelFilter> LabelFilters { get; }
		IReadOnlyCollection<IBitrateFilter> BitrateFilters { get; }
	}
}
