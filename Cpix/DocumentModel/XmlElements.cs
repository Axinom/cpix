using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Axinom.Cpix.DocumentModel
{
	[XmlRoot("CPIX", Namespace = Constants.CpixNamespace)]
	public sealed class DocumentRootElement
	{
		[XmlElement]
		public List<DeliveryDataElement> DeliveryData { get; set; } = new List<DeliveryDataElement>();

		[XmlElement("ContentKey")]
		public List<ContentKeyElement> ContentKeys { get; set; } = new List<ContentKeyElement>();
	}

	[XmlRoot("ContentKey", Namespace = Constants.CpixNamespace)]
	public sealed class ContentKeyElement
	{
		[XmlAttribute("keyId")]
		public string KeyId { get; set; }

		[XmlAttribute]
		public string Algorithm { get; set; }

		[XmlElement]
		public DataElement Data { get; set; }

		/// <summary>
		/// Performs basic sanity check to ensure that all required fields are filled.
		/// </summary>
		internal void LoadTimeValidate(bool expectEncrpyedValue)
		{
			if (expectEncrpyedValue)
			{
				if (Data?.Secret?.EncryptedValue == null)
					throw new NotSupportedException("Expected ContentKey/Data/Secret/EncryptedValue element does not exist.");

				if (Data.Secret.EncryptedValue.EncryptionMethod?.Algorithm != Constants.Aes256CbcAlgorithm)
					throw new NotSupportedException("Only the following algorithm is supported for encrypting content keys: " + Constants.Aes256CbcAlgorithm);

				if (Data.Secret.EncryptedValue.CipherData?.CipherValue == null || Data.Secret.EncryptedValue.CipherData?.CipherValue.Length == 0)
					throw new NotSupportedException("ContentKey/Data/Secret/EncryptedValue/CipherData/CipherValue element is missing.");

				// 128-bit IV + 128-bit encrypted content key.
				var expectedLength = (128 + 128) / 8;

				if (Data.Secret.EncryptedValue.CipherData?.CipherValue.Length != expectedLength)
					throw new NotSupportedException("ContentKey/Data/Secret/EncryptedValue/CipherData/CipherValue element does not contain the expected number of bytes (" + expectedLength + ")");

				if (Data?.Secret?.ValueMAC == null)
					throw new NotSupportedException("Expected ContentKey/Data/Secret/ValueMAC element does not exist.");
			}
			else
			{
				if (Data?.Secret?.PlainValue == null)
					throw new NotSupportedException("Expected ContentKey/Data/Secret/PlainValue element does not exist.");

				// 128-bit content key.
				var expectedLength = 128 / 8;

				if (Data.Secret.PlainValue.Length != expectedLength)
					throw new NotSupportedException("ContentKey/Data/Secret/PlainValue element does not contain the expected number of bytes (" + expectedLength + ")");
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
				throw new NotSupportedException("DeliveryData/MACKey/Key element is missing.");

			if (Key.EncryptionMethod?.Algorithm != Constants.RsaOaepAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for encrypting the MAC key: " + Constants.RsaOaepAlgorithm);

			if (Key.CipherData?.CipherValue == null || Key.CipherData?.CipherValue.Length == 0)
				throw new NotSupportedException("DeliveryData/MACKey/Key/CipherData/CipherValue element is missing.");
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
				throw new NotSupportedException("DeliveryData/DocumentKey/Data element is missing.");

			if (Data.Secret?.EncryptedValue == null)
				throw new NotSupportedException("DeliveryData/DocumentKey/Data/Secret/EncryptedValue element is missing.");

			if (Data.Secret?.EncryptedValue.EncryptionMethod?.Algorithm != Constants.RsaOaepAlgorithm)
				throw new NotSupportedException("Only the following algorithm is supported for encrypting the document key: " + Constants.RsaOaepAlgorithm);

			if (Data.Secret?.EncryptedValue.CipherData?.CipherValue == null || Data.Secret?.EncryptedValue.CipherData?.CipherValue.Length == 0)
				throw new NotSupportedException("DeliveryData/DocumentKey/Data/Secret/CipherData/CipherValue element is missing.");
		}
	}

	public sealed class DeliveryKeyElement
	{
		[XmlElement(Namespace = Constants.XmlDigitalSignatureNamespace)]
		public X509Data X509Data { get; set; }
	}
}
