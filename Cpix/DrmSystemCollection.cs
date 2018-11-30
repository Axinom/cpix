using Axinom.Cpix.Internal;
using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace Axinom.Cpix
{
	sealed class DrmSystemCollection : EntityCollection<DrmSystem>
	{
		public const string ContainerXmlElementName = "DRMSystemList";

		public DrmSystemCollection(CpixDocument document) : base(document)
		{
		}

		internal override string ContainerName => ContainerXmlElementName;

		protected override XmlElement SerializeEntity(XmlDocument document, XmlNamespaceManager namespaces, XmlElement container, DrmSystem entity)
		{
			var drmSystemElement = new DrmSystemElement
			{
				SystemId = entity.SystemId,
				KeyId = entity.KeyId,
				Pssh = entity.Pssh,
				SmoothStreamingProtectionHeaderData = entity.SmoothStreamingProtectionHeaderData
			};

			if (entity.ContentProtectionData != null)
				drmSystemElement.ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes(entity.ContentProtectionData));

			if (entity.HdsSignalingData != null)
				drmSystemElement.HdsSignalingData = Convert.ToBase64String(Encoding.UTF8.GetBytes(entity.HdsSignalingData));

			if (entity.HlsSignalingData?.MasterPlaylistData != null)
			{
				drmSystemElement.HlsSignalingData.Add(new HlsSignalingDataElement
				{
					Playlist = HlsPlaylistType.Master,
					Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(entity.HlsSignalingData.MasterPlaylistData))
				});
			}

			if (entity.HlsSignalingData?.MediaPlaylistData != null)
			{
				drmSystemElement.HlsSignalingData.Add(new HlsSignalingDataElement
				{
					Playlist = HlsPlaylistType.Media,
					Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(entity.HlsSignalingData.MediaPlaylistData))
				});
			}

			return XmlHelpers.AppendChildAndReuseNamespaces(drmSystemElement, container);
		}

		protected override DrmSystem DeserializeEntity(XmlElement element, XmlNamespaceManager namespaces)
		{
			var drmSystemElement = XmlHelpers.Deserialize<DrmSystemElement>(element);

			drmSystemElement.LoadTimeValidate();

			var drmSystem = new DrmSystem
			{
				SystemId = drmSystemElement.SystemId,
				KeyId = drmSystemElement.KeyId,
				Pssh = drmSystemElement.Pssh,
				SmoothStreamingProtectionHeaderData = drmSystemElement.SmoothStreamingProtectionHeaderData
			};

			if (drmSystemElement.ContentProtectionData != null)
				drmSystem.ContentProtectionData = Encoding.UTF8.GetString(Convert.FromBase64String(drmSystemElement.ContentProtectionData));

			if (drmSystemElement.HdsSignalingData != null)
				drmSystem.HdsSignalingData = Encoding.UTF8.GetString(Convert.FromBase64String(drmSystemElement.HdsSignalingData));

			if (drmSystemElement.HlsSignalingData.Count > 0)
			{
				drmSystem.HlsSignalingData = new HlsSignalingData();

				var mediaPlaylistDataAsBase64 = drmSystemElement.HlsSignalingData
					.SingleOrDefault(d => d.Playlist == null || string.Equals(d.Playlist, HlsPlaylistType.Media, StringComparison.InvariantCulture))?.Value;

				var masterPlaylistDataAsBase64 = drmSystemElement.HlsSignalingData
					.SingleOrDefault(d => string.Equals(d.Playlist, HlsPlaylistType.Master, StringComparison.InvariantCulture))?.Value;

				if (mediaPlaylistDataAsBase64 != null)
					drmSystem.HlsSignalingData.MediaPlaylistData = Encoding.UTF8.GetString(Convert.FromBase64String(mediaPlaylistDataAsBase64));

				if (masterPlaylistDataAsBase64 != null)
					drmSystem.HlsSignalingData.MasterPlaylistData = Encoding.UTF8.GetString(Convert.FromBase64String(masterPlaylistDataAsBase64));
			}

			return drmSystem;
		}

		protected override void ValidateCollectionStateBeforeAdd(DrmSystem entity)
		{
			if (this.Any(i => i.SystemId == entity.SystemId && i.KeyId == entity.KeyId))
				throw new InvalidOperationException(
					"The collection already contains a DRM system signaling entry with the same system ID and content key ID combination.");

			base.ValidateCollectionStateBeforeAdd(entity);
		}

		internal override void ValidateCollectionStateAfterLoad()
		{
			base.ValidateCollectionStateAfterLoad();

			if (this.Select(i => new { i.SystemId, i.KeyId }).Distinct().Count() != LoadedItems.Count())
				throw new InvalidCpixDataException(
					"The collection contains multiple DRM system signaling entries with the same system ID and content key ID combination.");
		}
	}
}