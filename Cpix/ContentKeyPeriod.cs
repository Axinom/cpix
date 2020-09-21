using System;

namespace Axinom.Cpix
{
	public sealed class ContentKeyPeriod : Entity
	{
		/// <summary>
		/// Gets or sets the ID of the content key period. This must be unique
		/// within the scope of this document and the value must be a valid XML
		/// ID.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Get or sets the numerical index for the key period. Mutually exclusive
		/// with start and end.
		/// </summary>
		public int? Index { get; set; }

		/// <summary>
		/// Gets or sets the wall clock (Live) or media time (VOD) for the start time
		/// for the period. Mutually inclusive with end, and mutually exclusive with
		/// index.
		/// </summary>
		public DateTimeOffset? Start { get; set; }

		/// <summary>
		/// Gets or sets the wall clock (Live) or media time (VOD) for the end time
		/// for the period. Mutually inclusive with start, and mutually exclusive
		/// with index.
		/// </summary>
		public DateTimeOffset? End { get; set; }

		internal override void ValidateNewEntity(CpixDocument document)
		{
			ValidateEntity();
		}

		internal override void ValidateLoadedEntity(CpixDocument document)
		{
			ValidateEntity();
		}

		private void ValidateEntity()
		{
			var invalidIndexStartEndCombination =
				(Index == null && (Start == null || End == null)) ||
				(Index != null && (Start != null || End != null));

			if (invalidIndexStartEndCombination)
				throw new InvalidCpixDataException("A content key period must specify either only the index or both the start and end time.");
		}
	}
}