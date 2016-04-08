using Axinom.Cpix.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Serialization;

namespace Axinom.Cpix
{
	public sealed class CpixDocument
	{
		/// <summary>
		/// Certificates identifying all the recipients of the CPIX document.
		/// 
		/// If this list contains any recipients, the content keys will be encrypted for each recipient on save.
		/// If this list is empty, content keys will be saved in the clear and are readable by anyone.
		/// </summary>
		public List<X509Certificate2> Recipients { get; set; } = new List<X509Certificate2>();

		/// <summary>
		/// Certificate identifying an entity who has signed the CPIX document to authenticate it.
		/// 
		/// If this is non-null, the CPIX document will be signed with the provided certificate on save.
		/// To sign the CPIX document, the private key of the certificate's key pair must be available for use.
		/// </summary>
		public X509Certificate2 Signer { get; set; }

		/// <summary>
		/// The set of content keys present in the CPIX document.
		/// </summary>
		public List<ContentKey> Keys { get; set; } = new List<ContentKey>();

		/// <summary>
		/// Saves the CPIX document to a stream.
		/// </summary>
		public void Save(Stream stream)
		{
			ValidateState();

			var document = new DocumentRootElement();

			// If null, no encryption is used. when serializing content key elements 
			// If non-null, encryption is used when serializing content key elements.
			AesManaged aes = null;

			if (Recipients?.Count != 0)
			{
				// There are recipients specified, so we shall encrypt the data.
				// First, generate all the keys and provide the delivery data.

				// 256-bit key is desirable.
				var documentKey = new byte[256 / 8];
				_random.GetBytes(documentKey);

				// Initialize AES with the document key.
				// A unique IV will be generated later for every content key.
				aes = new AesManaged
				{
					BlockSize = 128,
					KeySize = 256,
					Key = documentKey,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.None
				};

				// Generate delivery data for each recipient.
				foreach (var recipient in Recipients.Where(r => r != null))
				{
					byte[] encryptedDocumentKey = ((RSACryptoServiceProvider)recipient.PublicKey.Key).Encrypt(documentKey, true);

					document.DeliveryData.Add(new DeliveryDataElement
					{
						DeliveryKey = new DeliveryKeyElement
						{
							X509Data = new X509Data
							{
								Certificate = recipient.GetRawCertData()
							}
						},
						DocumentKey = new DocumentKeyElement
						{
							Algorithm = Constants.Aes256CbcAlgorithm,
							Data = new DataElement
							{
								Secret = new SecretDataElement
								{
									EncryptedValue = new EncryptedXmlValue
									{
										EncryptionMethod = new EncryptionMethodDeclaration
										{
											Algorithm = Constants.RsaOaepAlgorithm
										},
										CipherData = new CipherDataContainer
										{
											CipherValue = encryptedDocumentKey
										}
									}
								}
							}
						}
					});
				}
			}

			foreach (var key in Keys.Where(k => k != null))
			{
				var element = new ContentKeyElement
				{
					Algorithm = Constants.ContentKeyAlgorithm,
					KeyId = key.Id.ToString(),
					Data = new DataElement
					{
						Secret = new SecretDataElement()
					}
				};

				if (aes == null)
				{
					// Keys are serialized in the clear.
					element.Data.Secret.PlainValue = key.Value;
				}
				else
				{
					// Keys are encrypted with the document key.

					// Unique IV is generated for every content key.
					var iv = new byte[128 / 8];
					_random.GetBytes(iv);

					aes.IV = iv;

					using (var encryptor = aes.CreateEncryptor())
					{
						var encryptedValue = encryptor.TransformFinalBlock(key.Value, 0, key.Value.Length);

						// NB! We prepend the IV to the value when saving an encrypted value.
						element.Data.Secret.EncryptedValue = new EncryptedXmlValue
						{
							CipherData = new CipherDataContainer
							{
								CipherValue = iv.Concat(encryptedValue).ToArray()
							},
							EncryptionMethod = new EncryptionMethodDeclaration
							{
								Algorithm = Constants.Aes256CbcAlgorithm
							}
						};

						// TODO: MAC
					}
				}

				document.ContentKeys.Add(element);
			}

			// Serialize to memory first, for post-processing.
			var intermediateXmlBuffer = new MemoryStream();
			var serializer = new XmlSerializer(typeof(DocumentRootElement));
			serializer.Serialize(intermediateXmlBuffer, document);
			intermediateXmlBuffer.Position = 0; // Seek back to beginning.

			if (Signer != null)
			{
				// A signature was specified, so sign it!

				// SignedXml wants to work with XmlDocument so let's reload our data back from the buffer.
				var xmlDocument = new XmlDocument();
				xmlDocument.Load(intermediateXmlBuffer);

				// There is some funny business that happens with certificates loaded from PFX files in .NET 4.6.2 Preview.
				// You can't use them with the RSA-SHA512 algorithm! It just complains about an invalid algorithm.
				// To get around it, simply export and re-import the key pair to a new instance of the RSA CSP.
				using (var signingKey = new RSACryptoServiceProvider())
				{
					var exportedSigningKey = ((RSACryptoServiceProvider)Signer.PrivateKey).ExportParameters(true);
					signingKey.ImportParameters(exportedSigningKey);

					var signedXml = new SignedXml(xmlDocument)
					{
						SigningKey = signingKey
					};

					var whatToSign = new Reference
					{
						// The CPIX spec says what is signed (the entire document), so no need to specify URI.
						Uri = "",

						// A nice strong algorithm without known weaknesses that are easily exploitable.
						DigestMethod = SignedXml.XmlDsigSHA512Url
					};

					// The signature is inside the signed data.
					whatToSign.AddTransform(new XmlDsigEnvelopedSignatureTransform());

					signedXml.AddReference(whatToSign);

					// A nice strong algorithm without known weaknesses that are easily exploitable.
					signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA512Url;

					// Canonical XML 1.0 (omit comments); I suppose it works fine, no deep thoughts about this.
					signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl;

					// Signer certificate must be delivered with the signature.
					signedXml.KeyInfo.AddClause(new KeyInfoX509Data(Signer));

					// Ready to sign! Let's go!
					signedXml.ComputeSignature();

					// Now stick the Signature element it generated back into the document and we are done.
					var signature = signedXml.GetXml();
					xmlDocument.DocumentElement.AppendChild(xmlDocument.ImportNode(signature, true));
				}

				// Save the document back to the intermediate buffer
				intermediateXmlBuffer = new MemoryStream();
				xmlDocument.Save(intermediateXmlBuffer);
				intermediateXmlBuffer.Position = 0;
			}

			// Finally, copy everything to the desired output.
			intermediateXmlBuffer.CopyTo(stream);
		}

		private void ValidateState()
		{
			if (Keys?.Count <= 0)
				throw new InvalidOperationException("CPIX document must contain at least 1 content key.");

			if (Keys.Any(k => k?.Value?.Length != 16))
				throw new InvalidOperationException("All content keys must be exactly 16 bytes long.");

			if (Signer != null && !Signer.HasPrivateKey)
				throw new InvalidOperationException("The private key must be available for the signer certificate.");
		}

		private static RandomNumberGenerator _random = RandomNumberGenerator.Create();
	}
}
