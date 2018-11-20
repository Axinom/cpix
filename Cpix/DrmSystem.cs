using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace Axinom.Cpix
{
	public sealed class DrmSystem : Entity
	{
		/// <summary>
		/// Gets or sets the unique ID of the DRM system. Refer to the DASH-IF System
		/// ID registry for a list of DRM system IDs.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the ID of the content key the DRM system references.
		/// </summary>
		public Guid KeyId { get; set; }

		/// <summary>
		/// Gets or sets the content protection data, which is the full well-formed
		/// XML fragment that shall be added under the ContentProtection element in a
		/// DASH manifest. This must be a UTF-8 XML string without a byte order mark,
		/// encoded as base64.
		/// </summary>
		public string ContentProtectionData { get; set; }

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
			if (Id == Guid.Empty)
				throw new InvalidCpixDataException("An ID must be provided for each DRM system.");

			if (KeyId == Guid.Empty)
				throw new InvalidCpixDataException("A content key ID must be provided for each DRM system.");

			if (!string.IsNullOrEmpty(ContentProtectionData))
			{
				try
				{
					new XPathDocument(XmlReader.Create(
						new MemoryStream(Convert.FromBase64String(ContentProtectionData)),
						new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }));
				}
				catch (Exception ex)
				{
					throw new InvalidCpixDataException(
						"The content protection data must be a base64-encoded XML string of a well-formed XML fragment. " +
						$"Make sure all XML namespaces are declared. Error details: {ex.Message}");
				}
			}
		}
	}
}