using Axinom.Cpix.Internal;
using System.Linq;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class UsageRuleCollection : EntityCollection<UsageRule>
	{
		public const string ContainerXmlElementName = "ContentKeyUsageRuleList";

		public UsageRuleCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => ContainerXmlElementName;

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, UsageRule entity)
		{
			var element = new UsageRuleElement
			{
				KeyId = entity.KeyId,
				IntendedTrackType = entity.IntendedTrackType
			};

			if (entity.VideoFilters?.Count > 0)
			{
				element.VideoFilters = entity.VideoFilters
					.Select(f => new VideoFilterElement
					{
						MinPixels = f.MinPixels,
						MaxPixels = f.MaxPixels,
						MinFps = f.MinFramesPerSecond,
						MaxFps = f.MaxFramesPerSecond,
						Wcg = f.WideColorGamut,
						Hdr = f.HighDynamicRange
					})
					.ToArray();
			}

			if (entity.AudioFilters?.Count > 0)
			{
				element.AudioFilters = entity.AudioFilters
					.Select(f => new AudioFilterElement
					{
						MinChannels = f.MinChannels,
						MaxChannels = f.MaxChannels
					})
					.ToArray();
			}

			if (entity.BitrateFilters?.Count > 0)
			{
				element.BitrateFilters = entity.BitrateFilters
					.Select(f => new BitrateFilterElement
					{
						MinBitrate = f.MinBitrate,
						MaxBitrate = f.MaxBitrate
					})
					.ToArray();
			}

			if (entity.LabelFilters?.Count > 0)
			{
				element.LabelFilters = entity.LabelFilters
					.Select(f => new LabelFilterElement
					{
						Label = f.Label
					})
					.ToArray();
			}

			if (entity.KeyPeriodFilters?.Count > 0)
			{
				element.KeyPeriodFilters = entity.KeyPeriodFilters
					.Select(f => new KeyPeriodElement()
					{
						PeriodId = f.PeriodId
					})
					.ToArray();
			}

			return XmlHelpers.AppendChildAndReuseNamespaces(element, container);
		}

		protected override UsageRule DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			var raw = XmlHelpers.Deserialize<UsageRuleElement>(element);
			raw.LoadTimeValidate();

			var rule = new UsageRule
			{
				KeyId = raw.KeyId,
				IntendedTrackType = raw.IntendedTrackType
			};

			// This disables all usage rule processing, basically, and treats this particular rule as read-only.
			// The unknown filters will be preserved unless the rule is removed, just no rules from this document can be used.
			if (raw.UnknownFilters?.Any() == true)
				rule.ContainsUnsupportedFilters = true;

			if (raw.VideoFilters?.Length > 0)
			{
				rule.VideoFilters = raw.VideoFilters
					.Select(f => new VideoFilter
					{
						MinPixels = f.MinPixels,
						MaxPixels = f.MaxPixels,
						MinFramesPerSecond = f.MinFps,
						MaxFramesPerSecond = f.MaxFps,
						WideColorGamut = f.Wcg,
						HighDynamicRange = f.Hdr
					})
					.ToList();
			}

			if (raw.AudioFilters?.Length > 0)
			{
				rule.AudioFilters = raw.AudioFilters
					.Select(f => new AudioFilter
					{
						MinChannels = f.MinChannels,
						MaxChannels = f.MaxChannels
					})
					.ToList();
			}

			if (raw.BitrateFilters?.Length > 0)
			{
				rule.BitrateFilters = raw.BitrateFilters
					.Select(f => new BitrateFilter
					{
						MinBitrate = f.MinBitrate,
						MaxBitrate = f.MaxBitrate
					})
					.ToList();
			}

			if (raw.LabelFilters?.Length > 0)
			{
				rule.LabelFilters = raw.LabelFilters
					.Select(f => new LabelFilter
					{
						Label = f.Label
					})
					.ToList();
			}

			if (raw.KeyPeriodFilters?.Length > 0)
			{
				rule.KeyPeriodFilters = raw.KeyPeriodFilters
					.Select(f => new KeyPeriodFilter
					{
						PeriodId = f.PeriodId
					})
					.ToList();
			}
			
			return rule;
		}
	}
}
