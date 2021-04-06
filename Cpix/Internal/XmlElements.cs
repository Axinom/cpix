using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Axinom.Cpix.Internal
{
	// We stash these in a sub-namespace since XmlSerializer tends to expect types to be public (and workarounds are not nice).
	//
	// Nullable types are not well supported by XmlSerializer, so we use the idiom of *AsXmlString to manually serialize
	// to/from the string-form of the data. XmlSerializer just converts between XML strings and string objects there.

	/// <summary>
	/// Just for creating a new blank document and handling root attributes. Other
	/// serialization is performed independently.
	/// </summary>
	[XmlRoot("CPIX", Namespace = Constants.CpixNamespace)]
	public sealed class DocumentRootElement
	{
		public const string ContentIdAttributeName = "contentId";
		public const string VersionAttributeName = "version";

		[XmlAttribute(ContentIdAttributeName)]
		public string ContentId { get; set; }

		[XmlAttribute(VersionAttributeName)]
		public string Version { get; set; }
	}

	#region Usage rules
	[XmlRoot("ContentKeyUsageRule", Namespace = Constants.CpixNamespace)]
	public sealed class UsageRuleElement
	{
		[XmlAttribute("kid")]
		public Guid KeyId { get; set; }

		[XmlAttribute("intendedTrackType")]
		public string IntendedTrackType { get; set; }

		[XmlElement("KeyPeriodFilter")]
		public KeyPeriodElement[] KeyPeriodFilters { get; set; }

		[XmlElement("LabelFilter")]
		public LabelFilterElement[] LabelFilters { get; set; }

		[XmlElement("VideoFilter")]
		public VideoFilterElement[] VideoFilters { get; set; }

		[XmlElement("AudioFilter")]
		public AudioFilterElement[] AudioFilters { get; set; }

		[XmlElement("BitrateFilter")]
		public BitrateFilterElement[] BitrateFilters { get; set; }

		[XmlAnyElement]
		public XmlElement[] UnknownFilters { get; set; }

		internal void LoadTimeValidate()
		{
			// Malformed rules will not work right for resolving but who are we to say what is malformed or not.
			// Nothing to really validate here, in essence. Go wild!
		}
	}

	public sealed class KeyPeriodElement
	{
		[XmlAttribute("periodId")]
		public string PeriodId { get; set; }
	}

	public sealed class LabelFilterElement
	{
		[XmlAttribute("label")]
		public string Label { get; set; }
	}

	public sealed class VideoFilterElement
	{
		[XmlIgnore]
		public long? MinPixels { get; set; }

		[XmlIgnore]
		public long? MaxPixels { get; set; }

		[XmlAttribute("minPixels")]
		public string MinPixelsAsXmlString
		{
			get { return MinPixels?.ToString(); }
			set { MinPixels = value != null ? (long?)long.Parse(value) : null; }
		}

		[XmlAttribute("maxPixels")]
		public string MaxPixelsAsXmlString
		{
			get { return MaxPixels?.ToString(); }
			set { MaxPixels = value != null ? (long?)long.Parse(value) : null; }
		}

		[XmlIgnore]
		public bool? Hdr { get; set; }

		[XmlIgnore]
		public bool? Wcg { get; set; }

		[XmlAttribute("hdr")]
		public string HdrAsXmlString
		{
			get { return Hdr?.ToString().ToLowerInvariant(); }
			set { Hdr = value != null ? (bool?)bool.Parse(value) : null; }
		}

		[XmlAttribute("wcg")]
		public string WcgAsXmlString
		{
			get { return Wcg?.ToString().ToLowerInvariant(); }
			set { Wcg = value != null ? (bool?)bool.Parse(value) : null; }
		}

		[XmlIgnore]
		public long? MinFps { get; set; }

		[XmlIgnore]
		public long? MaxFps { get; set; }

		[XmlAttribute("minFps")]
		public string MinFpsAsXmlString
		{
			get { return MinFps?.ToString(); }
			set { MinFps = value != null ? (long?)long.Parse(value) : null; }
		}

		[XmlAttribute("maxFps")]
		public string MaxFpsAsXmlString
		{
			get { return MaxFps?.ToString(); }
			set { MaxFps = value != null ? (long?)long.Parse(value) : null; }
		}
	}

	public sealed class AudioFilterElement
	{
		[XmlIgnore]
		public int? MinChannels { get; set; }

		[XmlIgnore]
		public int? MaxChannels { get; set; }

		[XmlAttribute("minChannels")]
		public string MinChannelsAsXmlString
		{
			get { return MinChannels?.ToString(); }
			set { MinChannels = value != null ? (int?)int.Parse(value) : null; }
		}

		[XmlAttribute("maxChannels")]
		public string MaxChannelsAsXmlString
		{
			get { return MaxChannels?.ToString(); }
			set { MaxChannels = value != null ? (int?)int.Parse(value) : null; }
		}
	}

	public sealed class BitrateFilterElement
	{
		[XmlIgnore]
		public long? MinBitrate { get; set; }

		[XmlIgnore]
		public long? MaxBitrate { get; set; }

		[XmlAttribute("minBitrate")]
		public string MinBitrateAsXmlString
		{
			get { return MinBitrate?.ToString(); }
			set { MinBitrate = value != null ? (long?)long.Parse(value) : null; }
		}

		[XmlAttribute("maxBitrate")]
		public string MaxBitrateAsXmlString
		{
			get { return MaxBitrate?.ToString(); }
			set { MaxBitrate = value != null ? (long?)long.Parse(value) : null; }
		}
	}
	#endregion

	#region DRM systems
	[XmlRoot("DRMSystem", Namespace = Constants.CpixNamespace)]
	public sealed class DrmSystemElement
	{
		[XmlAttribute("systemId")]
		public Guid SystemId { get; set; }

		[XmlAttribute("kid")]
		public Guid KeyId { get; set; }

		[XmlElement("PSSH")]
		public string Pssh { get; set; }

		[XmlElement]
		public string ContentProtectionData { get; set; }

		[XmlElement("URIExtXKey")]
		public string UriExtXKey { get; set; }

		[XmlElement("HLSSignalingData")]
		public List<HlsSignalingDataElement> HlsSignalingData { get; set; } = new List<HlsSignalingDataElement>();

		[XmlElement]
		public string SmoothStreamingProtectionHeaderData { get; set; }

		[XmlElement("HDSSignalingData")]
		public string HdsSignalingData { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate()
		{
			if (HlsSignalingData.Any(d => d.Playlist == null) && HlsSignalingData.Count > 1)
			{
				throw new InvalidCpixDataException(
					"There can be only one HLSSignalingData element when the playlist attribute is not present for any such element.");
			}
		}
	}

	[XmlRoot("DRMSystem", Namespace = Constants.CpixNamespace)]
	public sealed class HlsSignalingDataElement
	{
		[XmlAttribute("playlist")]
		public string Playlist { get; set; }

		[XmlText]
		public string Value { get; set; } = string.Empty;
	}
	#endregion

	#region Content key periods
	[XmlRoot("ContentKeyPeriod", Namespace = Constants.CpixNamespace)]
	public sealed class ContentKeyPeriodElement
	{
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlIgnore]
		public int? Index { get; set; }

		[XmlAttribute("index")]
		public string IndexAsXmlString
		{
			get => Index?.ToString();
			set => Index = value != null ? (int?)int.Parse(value) : null;
		}

		[XmlIgnore]
		public DateTimeOffset? Start { get; set; }

		[XmlAttribute("start")]
		public string StartAsXmlString
		{
			// XML date-times must be in the ISO 8601 ("O") format. Also, we'll allow
			// missing timezones in order to throw less errors. But missing ones are
			// considered as UTC.
			get => Start?.ToString("O"); 
			set => Start = value != null ? (DateTimeOffset?)DateTimeOffset.Parse(value, null, DateTimeStyles.AssumeUniversal) : null;
		}

		[XmlIgnore]
		public DateTimeOffset? End { get; set; }

		[XmlAttribute("end")]
		public string EndAsXmlString
		{
			get => End?.ToString("O");
			set => End = value != null ? (DateTimeOffset?)DateTimeOffset.Parse(value, null, DateTimeStyles.AssumeUniversal) : null;
		}
	}

	#endregion

	#region Content keys
	[XmlRoot("ContentKey", Namespace = Constants.CpixNamespace)]
	public sealed class ContentKeyElement
	{
		[XmlAttribute("kid")]
		public Guid KeyId { get; set; }

		[XmlAttribute("explicitIV")]
		public byte[] ExplicitIv { get; set; }

		[XmlAttribute("commonEncryptionScheme")]
		public string CommonEncryptionScheme { get; set; }

		[XmlElement]
		public DataElement Data { get; set; }

		internal bool HasEncryptedValue => Data?.Secret?.EncryptedValue != null;
		internal bool HasPlainValue => Data?.Secret?.PlainValue != null;

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// Both encrypted or clear values are acceptable (but not both at the same time).
		/// </summary>
		internal void LoadTimeValidate()
		{
			if (HasEncryptedValue && HasPlainValue)
				throw new InvalidCpixDataException("Cannot have both ContentKey/Data/Secret/EncryptedValue and ContentKey/Data/Secret/PlainValue! Is it encrypted or not?");

			if (HasEncryptedValue)
			{
				if (Data.Secret.EncryptedValue.EncryptionMethod?.Algorithm != Constants.Aes256CbcAlgorithm)
					throw new NotSupportedException("Only the following algorithm is supported for encrypting content keys: " + Constants.Aes256CbcAlgorithm);

				if (Data.Secret.EncryptedValue.CipherData?.CipherValue == null || Data.Secret.EncryptedValue.CipherData?.CipherValue.Length == 0)
					throw new InvalidCpixDataException("ContentKey/Data/Secret/EncryptedValue/CipherData/CipherValue element is missing.");

				// 128-bit IV + 128-bit encrypted content key + 128-bit PKCS#7 padding block.
				var expectedLength = (128 + 128 + 128) / 8;

				if (Data.Secret.EncryptedValue.CipherData?.CipherValue.Length != expectedLength)
					throw new InvalidCpixDataException("ContentKey/Data/Secret/EncryptedValue/CipherData/CipherValue element does not contain the expected number of bytes (" + expectedLength + ")");

				if (Data?.Secret?.ValueMAC == null)
					throw new NotSupportedException("Expected ContentKey/Data/Secret/ValueMAC element does not exist.");
			}
			else if (HasPlainValue)
			{
				// 128-bit content key.
				var expectedLength = 128 / 8;

				if (Data.Secret.PlainValue.Length != expectedLength)
					throw new InvalidCpixDataException("ContentKey/Data/Secret/PlainValue element does not contain the expected number of bytes (" + expectedLength + ")");
			}
			else
			{
				// Note: This library originally considered the content key element not having any
				// key data an error and this case threw an error ("Must have either
				// ContentKey/Data/Secret/EncryptedValue or ContentKey/Data/Secret/PlainValue.").
				// The 'data' element was also required at the schema level. However, according to
				// official spec and schema content key data is optional. And in practice there's a
				// demand for using CPIX for key request purposes where content key IDs may need to
				// be present, but data yet really cannot be.
				//
				// Counter-argument from at least one of the CPIX authors (the same who created this
				// library; see https://github.com/Dash-Industry-Forum/CPIX/issues/88) is that CPIX
				// should only be used for key transportation and not for key requests, which would
				// make key elements without data meaningless. Since the library likely was developed
				// with this requirement in mind, there's some danger that the removal of this
				// requirement may "destabilize" some flows. However, running automatic and brief
				// manual tests revealed no problems when removing just the on-load validation.
				//
				// Therefore, let's not require content key data on-load, but keep the requirement
				// when adding new content keys to avoid compromising some of the existing
				// functionality. This is also more in line with the library's philosophy: be strict
				// when creating a new document, but more lenient when loading existing documents.
			}
		}
	}
	#endregion

	#region Cryptographic elements (shared)
	[XmlRoot(Namespace = Constants.PskcNamespace)]
	public sealed class DataElement
	{
		/// <summary>
		/// For our purposes, this is just a meaningless layer of nesting.
		/// The content key is in this regardless of whether it is encrypted or not.
		/// </summary>
		[XmlElement]
		public SecretDataElement Secret { get; set; }
	}

	public sealed class SecretDataElement
	{
		/// <summary>
		/// Indicates that the data is a plain (nonencrypted) value.
		/// </summary>
		[XmlElement]
		public byte[] PlainValue { get; set; }

		[XmlElement]
		public EncryptedXmlValue EncryptedValue { get; set; }

		[XmlElement]
		public byte[] ValueMAC { get; set; }
	}

	public sealed class EncryptedXmlValue
	{
		[XmlElement("EncryptionMethod", Namespace = Constants.XmlEncryptionNamespace)]
		public EncryptionMethodDeclaration EncryptionMethod { get; set; }

		[XmlElement(Namespace = Constants.XmlEncryptionNamespace)]
		public CipherDataContainer CipherData { get; set; }
	}

	public sealed class CipherDataContainer
	{
		[XmlElement(Namespace = Constants.XmlEncryptionNamespace)]
		public byte[] CipherValue { get; set; }
	}

	public sealed class EncryptionMethodDeclaration
	{
		[XmlAttribute]
		public string Algorithm { get; set; }
	}
	#endregion

	#region Delivery data
	[XmlRoot("DeliveryData", Namespace = Constants.CpixNamespace)]
	public sealed class DeliveryDataElement
	{
		[XmlElement]
		public DeliveryKeyElement DeliveryKey { get; set; }

		[XmlElement]
		public DocumentKeyElement DocumentKey { get; set; }

		[XmlElement("MACMethod")]
		public MacMethodElement MacMethod { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate()
		{
			// DeliveryKey was already checked by CpixDocument or we would not have gotten here.

			if (DocumentKey == null)
				throw new NotSupportedException("DeliveryData/DocumentKey element is missing.");

			if (MacMethod == null)
				throw new NotSupportedException("DeliveryData/MacMethod element is missing.");

			DocumentKey.LoadTimeValidate();
			MacMethod.LoadTimeValidate();
		}
	}

	public sealed class MacMethodElement
	{
		[XmlAttribute]
		public string Algorithm { get; set; }

		[XmlElement("Key")]
		public EncryptedXmlValue Key { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate()
		{
			if (Algorithm != Constants.HmacSha512Algorithm)
				throw new NotSupportedException("Only the following algorithm is supported for MAC generation: " + Constants.HmacSha512Algorithm);

			if (Key == null)
				throw new InvalidCpixDataException("DeliveryData/MacMethod/Key element is missing.");

			if (Key.EncryptionMethod?.Algorithm != Constants.RsaOaepAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for encrypting the MAC key: " + Constants.RsaOaepAlgorithm);

			if (Key.CipherData?.CipherValue == null || Key.CipherData?.CipherValue.Length == 0)
				throw new InvalidCpixDataException("DeliveryData/MacMethod/Key/CipherData/CipherValue element is missing.");
		}
	}

	public sealed class DocumentKeyElement
	{
		[XmlAttribute]
		public string Algorithm { get; set; }

		[XmlElement]
		public DataElement Data { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate()
		{
			if (Algorithm != Constants.Aes256CbcAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for the document key: " + Constants.Aes256CbcAlgorithm);

			if (Data == null)
				throw new InvalidCpixDataException("DeliveryData/DocumentKey/Data element is missing.");

			if (Data.Secret?.EncryptedValue == null)
				throw new InvalidCpixDataException("DeliveryData/DocumentKey/Data/Secret/EncryptedValue element is missing.");

			if (Data.Secret?.EncryptedValue.EncryptionMethod?.Algorithm != Constants.RsaOaepAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for encrypting the document key: " + Constants.RsaOaepAlgorithm);

			if (Data.Secret?.EncryptedValue.CipherData?.CipherValue == null || Data.Secret?.EncryptedValue.CipherData?.CipherValue.Length == 0)
				throw new InvalidCpixDataException("DeliveryData/DocumentKey/Data/Secret/CipherData/CipherValue element is missing.");
		}
	}

	public sealed class DeliveryKeyElement
	{
		[XmlElement(Namespace = Constants.XmlDigitalSignatureNamespace)]
		public X509Data X509Data { get; set; }
	}

	public sealed class X509Data
	{
		[XmlElement("X509Certificate")]
		public byte[] Certificate { get; set; }
	}
	#endregion
}
