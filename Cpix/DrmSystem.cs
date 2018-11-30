using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Axinom.Cpix
{
	public sealed class DrmSystem : Entity
	{
		/// <summary>
		/// Gets or sets the system ID of the DRM system that is being signaled.
		/// Refer to the DASH-IF System ID registry for a list of DRM system IDs.
		/// </summary>
		public Guid SystemId { get; set; }

		/// <summary>
		/// Gets or sets the ID of the content key the DRM system signaling entry
		/// references.
		/// </summary>
		public Guid KeyId { get; set; }

		/// <summary>
		/// Gets or sets the full PSSH box, encoded as base64, that shall be added to
		/// ISOBMFF files encrypted with the content key referenced by the DRM system
		/// signaling entry. This data should only be associated with a hierarchical
		/// leaf key.
		/// </summary>
		public string Pssh { get; set; }

		/// <summary>
		/// Gets or sets the content protection data, which is the full well-formed
		/// XML fragment that shall be added under the ContentProtection element in a
		/// DASH manifest. This must be a UTF-8 XML string without a byte order mark.
		/// This data shall not be associated with a hierarchical leaf key.
		/// </summary>
		public string ContentProtectionData { get; set; }

		/// <summary>
		/// Gets or sets the HLS signaling data to be inserted in HLS master and/or
		/// media playlists. The data includes #EXT-X-KEY or #EXT-X-SESSION-KEY
		/// tags (depending on the playlist), along with potential proprietary tags.
		/// This data shall not be associated with a hierarchical leaf key.
		/// </summary>
		public HlsSignalingData HlsSignalingData { get; set; }

		/// <summary>
		/// Gets or sets the Smooth Streaming Protection Header data, to be used as
		/// the inner text of the ProtectionHeader XML element in a Smooth Streaming
		/// manifest. This is UTF-8 text without a byte order mark. This data shall
		/// not be associated with a hierarchical leaf key.
		/// </summary>
		public string SmoothStreamingProtectionHeaderData { get; set; }

		/// <summary>
		/// Gets or sets the HDS signaling data, which is the full
		/// "drmAdditionalHeader" XML element, intended to be used in a Flash media
		/// manifest. This is a UTF-8 XML string without a byte order mark. This data
		/// shall not be associated with a hierarchical leaf key.
		/// </summary>
		public string HdsSignalingData { get; set; }


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
			if (SystemId == Guid.Empty)
				throw new InvalidCpixDataException("A system ID must be provided for each DRM system signaling entry.");

			if (KeyId == Guid.Empty)
				throw new InvalidCpixDataException("A content key ID must be provided for each DRM system signaling entry.");

			if (Pssh != null)
			{
				try
				{
					var temp = Convert.FromBase64String(Pssh);
				}
				catch (Exception ex)
				{
					throw new InvalidCpixDataException("The PSSH must be base64-encoded.", ex);
				}
			}

			if (ContentProtectionData != null)
			{
				try
				{
					var temp = new XPathDocument(XmlReader.Create(
						new MemoryStream(Encoding.UTF8.GetBytes(ContentProtectionData)),
						new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }));
				}
				catch (Exception ex)
				{
					throw new InvalidCpixDataException(
						$"The content protection data must be a well-formed XML fragment. Error details: {ex.Message}", ex);
				}
			}

			if (HdsSignalingData != null)
			{
				XPathDocument document;

				try
				{
					document = new XPathDocument(XmlReader.Create(
						new MemoryStream(Encoding.UTF8.GetBytes(HdsSignalingData)),
						new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Document }));
				}
				catch (Exception ex)
				{
					throw new InvalidCpixDataException(
						$"The HDS signaling data must be a well-formed XML element. Error details: {ex.Message}", ex);
				}

				var navigator = document.CreateNavigator();

				if (!(navigator.MoveToFirstChild() && navigator.LocalName.Equals("drmAdditionalHeader", StringComparison.InvariantCulture)))
				{
					throw new InvalidCpixDataException("The HDS signaling data must be the full \"drmAdditionalHeader\" XML element.");
				}
			}
		}
	}
}