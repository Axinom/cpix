using System;
using System.Collections.Generic;
using System.Linq;

namespace Axinom.Cpix
{
	public sealed class UsageRule : Entity
	{
		/// <summary>
		/// The ID of the content key that this usage rule applies to.
		/// </summary>
		public Guid KeyId { get; set; }

		/// <summary>
		/// If true for a loaded usage rule, there were filters present in the CPIX document that are not supported
		/// by the current implementation. Presence of such filters disables usage rule resolving for the entire document.
		/// </summary>
		public bool ContainsUnsupportedFilters { get; internal set; }

		public ICollection<KeyPeriodFilter> KeyPeriodFilters { get; set; } = new List<KeyPeriodFilter>();
		public ICollection<VideoFilter> VideoFilters { get; set; } = new List<VideoFilter>();
		public ICollection<AudioFilter> AudioFilters { get; set; } = new List<AudioFilter>();
		public ICollection<LabelFilter> LabelFilters { get; set; } = new List<LabelFilter>();
		public ICollection<BitrateFilter> BitrateFilters { get; set; } = new List<BitrateFilter>();
		
		internal override void ValidateNewEntity(CpixDocument document)
		{
			// This can happen if an entity with unsupported filters gets re-added to a document, for some misguided reason.
			if (ContainsUnsupportedFilters)
				throw new InvalidCpixDataException("Cannot add a content key usage rule that contains unsupported filters. Such usage rules can only be passed through unmodified when processing a CPIX document.");

			ValidateLoadedEntity(document);
		}

		internal override void ValidateLoadedEntity(CpixDocument document)
		{
			if (!document.ContentKeys.Any(ck => ck.Id == KeyId))
				throw new InvalidCpixDataException("Content key usage rule references a content key that is not present in the CPIX document.");

			foreach (var keyPeriodFilter in KeyPeriodFilters)
			{
				keyPeriodFilter.Validate();

				if (!document.ContentKeyPeriods.Any(ckp => ckp.Id == keyPeriodFilter.PeriodId))
					throw new InvalidCpixDataException("Content key usage rule key period filter references a content key period that is not present in the CPIX document.");
			}

			foreach (var videoFilter in VideoFilters)
				videoFilter.Validate();

			foreach (var audioFilter in AudioFilters)
				audioFilter.Validate();

			foreach (var labelFilter in LabelFilters)
				labelFilter.Validate();

			foreach (var bitrateFilter in BitrateFilters)
				bitrateFilter.Validate();
		}
	}
}
