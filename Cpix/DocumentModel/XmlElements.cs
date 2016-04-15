using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Serialization;

namespace Axinom.Cpix.DocumentModel
{
	/// <summary>
	/// We just use this to serialize content keys - hence it intentionally does not contain any of the other elements.
	/// </summary>
	[XmlRoot("CPIX", Namespace = Constants.CpixNamespace)]
	public sealed class DocumentRootElement
	{
		[XmlElement]
		public List<DeliveryDataElement> DeliveryData { get; set; } = new List<DeliveryDataElement>();

		[XmlElement("ContentKey")]
		public List<ContentKeyElement> ContentKeys { get; set; } = new List<ContentKeyElement>();
	}

	[XmlRoot("ContentKeyAssignmentRule", Namespace = Constants.CpixNamespace)]
	public sealed class AssignmentRuleElement
	{
		[XmlAttribute("keyId")]
		public Guid KeyId { get; set; }

		[XmlElement]
		public TimeFilterElement TimeFilter { get; set; }

		[XmlElement]
		public CryptoPeriodFilterElement CryptoPeriodFilter { get; set; }

		[XmlElement]
		public LabelFilterElement LabelFilter { get; set; }

		[XmlElement]
		public VideoFilterElement VideoFilter { get; set; }

		[XmlElement]
		public AudioFilterElement AudioFilter { get; set; }

		[XmlElement]
		public BitrateFilterElement BitrateFilter { get; set; }
	}

	public sealed class TimeFilterElement
	{
		[XmlIgnore]
		public DateTimeOffset? Start { get; set; }

		[XmlIgnore]
		public DateTimeOffset? End { get; set; }

		[XmlAttribute("start")]
		public string StartAsXmlString
		{
			get { return Start?.ToString("o"); }
			set { Start = value == null ? null : (DateTimeOffset?)DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture); }
		}

		[XmlAttribute("end")]
		public string EndAsXmlString
		{
			get { return End?.ToString("o"); }
			set { End = value == null ? null : (DateTimeOffset?)DateTimeOffset.ParseExact(value, "o", CultureInfo.InvariantCulture); }
		}
	}

	public sealed class CryptoPeriodFilterElement
	{
		[XmlAttribute("periodIndex")]
		public long PeriodIndex { get; set; }
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

	[XmlRoot("ContentKey", Namespace = Constants.CpixNamespace)]
	public sealed class ContentKeyElement
	{
		[XmlAttribute("id")]
		public string XmlId { get; set; }

		[XmlAttribute("keyId")]
		public Guid KeyId { get; set; }

		[XmlAttribute]
		public string Algorithm { get; set; }

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

				// 128-bit IV + 128-bit encrypted content key.
				var expectedLength = (128 + 128) / 8;

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
				throw new InvalidCpixDataException("Must have either ContentKey/Data/Secret/EncryptedValue or ContentKey/Data/Secret/PlainValue.");
			}
		}
	}

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

	public sealed class X509Data
	{
		[XmlElement("X509Certificate")]
		public byte[] Certificate { get; set; }
	}

	[XmlRoot("DeliveryData", Namespace = Constants.CpixNamespace)]
	public sealed class DeliveryDataElement
	{
		[XmlElement]
		public DeliveryKeyElement DeliveryKey { get; set; }

		[XmlElement]
		public DocumentKeyElement DocumentKey { get; set; }

		[XmlElement("MACKey")]
		public MacKey MacKey { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate()
		{
			// DeliveryKey was already checked by CpixDocument we would not have gotten here.

			if (DocumentKey == null)
				throw new NotSupportedException("DeliveryData/DocumentKey element is missing.");

			if (MacKey == null)
				throw new NotSupportedException("DeliveryData/MACKey element is missing.");

			DocumentKey.LoadTimeValidate();
			MacKey.LoadTimeValidate();
		}
	}

	public sealed class MacKey
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
				throw new InvalidCpixDataException("DeliveryData/MACKey/Key element is missing.");

			if (Key.EncryptionMethod?.Algorithm != Constants.RsaOaepAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for encrypting the MAC key: " + Constants.RsaOaepAlgorithm);

			if (Key.CipherData?.CipherValue == null || Key.CipherData?.CipherValue.Length == 0)
				throw new InvalidCpixDataException("DeliveryData/MACKey/Key/CipherData/CipherValue element is missing.");
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
}
