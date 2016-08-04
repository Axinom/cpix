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

		/// <summary>
		/// If true for a loaded usage rule, there were filters present in the CPIX document that are not supported
		/// by the current implementation. Presence of such filters disables usage rule resolving for the entire document.
		/// </summary>
		bool ContainsUnsupportedFilters { get; }

		IReadOnlyCollection<IVideoFilter> VideoFilters { get; }
		IReadOnlyCollection<IAudioFilter> AudioFilters { get; }
		IReadOnlyCollection<ILabelFilter> LabelFilters { get; }
		IReadOnlyCollection<IBitrateFilter> BitrateFilters { get; }
	}
}
