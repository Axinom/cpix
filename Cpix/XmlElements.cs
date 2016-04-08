using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

	public sealed class ContentKeyElement
	{
		[XmlAttribute("keyId")]
		public string KeyId { get; set; }

		[XmlAttribute]
		public string Algorithm { get; set; }

		[XmlElement]
		public DataElement Data { get; set; }
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

		// TODO: MAC handling
		/*[XmlElement]
		public byte[] ValueMAC { get; set; }*/
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

	public sealed class DeliveryDataElement
	{
		[XmlElement]
		public DeliveryKeyElement DeliveryKey { get; set; }

		[XmlElement]
		public DocumentKeyElement DocumentKey { get; set; }
	}

	public sealed class DocumentKeyElement
	{
		[XmlAttribute]
		public string Algorithm { get; set; }

		[XmlElement]
		public DataElement Data { get; set; }
	}

	public sealed class DeliveryKeyElement
	{
		[XmlElement(Namespace = Constants.XmlDigitalSignatureNamespace)]
		public X509Data X509Data { get; set; }
	}
}
