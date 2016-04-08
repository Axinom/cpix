using Axinom.Cpix.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
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
		/// Certificates identifying all the recipients of the CPIX document. This list is not populated on load.
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
		/// Loads a CPIX document from a stream, decrypting it using the key pairs of the providede certificates, if required.
		/// </summary>
		public static CpixDocument Load(Stream stream, ICollection<X509Certificate2> decryptionCertificates = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (decryptionCertificates != null && decryptionCertificates.Any(c => c?.HasPrivateKey != true))
				throw new ArgumentException("The private keys associated with all provided decryption certificates must be available.");

			var cpix = new CpixDocument();

			var xmlDocument = new XmlDocument();
			xmlDocument.Load(stream);

			var xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
			xmlNamespaceManager.AddNamespace("cpix", Constants.CpixNamespace);
			xmlNamespaceManager.AddNamespace("pskc", Constants.PskcNamespace);
			xmlNamespaceManager.AddNamespace("enc", Constants.XmlEncryptionNamespace);
			xmlNamespaceManager.AddNamespace("ds", Constants.XmlDigitalSignatureNamespace);

			// First check for and validate the signature.
			var signatureNode = xmlDocument.SelectSingleNode("/cpix:CPIX/ds:Signature", xmlNamespaceManager);

			if (signatureNode != null)
			{
				// There is a signature!

				var signedXml = new SignedXml(xmlDocument);
				signedXml.LoadXml((XmlElement)signatureNode);

				// The signer certificate must be provided or we won't play ball.
				var signerCertificateNode = signatureNode.SelectSingleNode("ds:KeyInfo/ds:X509Data/ds:X509Certificate", xmlNamespaceManager);

				if (signerCertificateNode == null)
					throw new NotSupportedException("An XML digital signature was found on the CPIX document but the X.509 certificate of the signer was not included.");

				// We just verify the signature. Whether the caller trusts the signer is another thing entirely.
				var signerCertificate = new X509Certificate2(Convert.FromBase64String(signerCertificateNode.InnerText));
				if (!signedXml.CheckSignature(signerCertificate, true))
					throw new SecurityException("Digital signature verification failed - the CPIX document has been tampered with!");

				cpix.Signer = signerCertificate;
			}

			// Now look for and acquire the document key so that the encrypted data can be decrypted.
			// There is no provision in this library for loading encrypted data without decrypting it (pass-through).
			var deliveryDataNodes = xmlDocument.SelectNodes("cpix:CPIX/cpix:DeliveryData", xmlNamespaceManager);

			// If content keys are encrypted, we fill these and use for later cryptography.
			AesManaged aes = null;
			HMACSHA512 mac = null;

			if (deliveryDataNodes.Count != 0)
			{
				// The document is encrypted. We need to find delivery data that we can access.
				foreach (XmlElement deliveryDataNode in deliveryDataNodes)
				{
					// The delivery key must contain our X509 certificate or we will consider it a non-match.
					var deliveryCertificateNode = deliveryDataNode.SelectSingleNode("cpix:DeliveryKey/ds:X509Data/ds:X509Certificate", xmlNamespaceManager);

					if (deliveryCertificateNode == null)
						continue; // Huh? Okay, whatever. Ignore it.

					var deliveryCertificate = new X509Certificate2(Convert.FromBase64String(deliveryCertificateNode.InnerText));

					var decryptionCertificate = decryptionCertificates.FirstOrDefault(c => c.Thumbprint == deliveryCertificate.Thumbprint);

					if (decryptionCertificate == null)
						continue; // Nope. Next, please.

					// This delivery data is for us!
					// Deserialize this DeliveryData into a nice structure for easier processing.
					var deliveryData = XmlElementToXmlDeserialized<DeliveryDataElement>(deliveryDataNode);

					// Verify that all the values make sense.
					deliveryData.LoadTimeValidate();

					var rsa = (RSACryptoServiceProvider)decryptionCertificate.PrivateKey;
					var macKey = rsa.Decrypt(deliveryData.MacKey.Key.CipherData.CipherValue, true);
					var documentKey = rsa.Decrypt(deliveryData.DocumentKey.Data.Secret.EncryptedValue.CipherData.CipherValue, true);

					aes = new AesManaged
					{
						BlockSize = 128,
						KeySize = 256,
						Key = documentKey,
						Mode = CipherMode.CBC,
						Padding = PaddingMode.None
					};

					mac = new HMACSHA512(macKey);

					// Found it, no need to keep going.
					break;
				}

				if (aes == null)
					throw new SecurityException("None of the provided certificates references a a key pair capable of derypting the CPIX document.");
			}

			// Preparations complete. Let's now load the content keys!
			var contentKeyNodes = xmlDocument.SelectNodes("/cpix:CPIX/cpix:ContentKey", xmlNamespaceManager);

			foreach (XmlElement contentKeyNode in contentKeyNodes)
			{
				// Deserialize for easier processing.
				var contentKey = XmlElementToXmlDeserialized<ContentKeyElement>(contentKeyNode);

				var encryptionIsUsed = aes != null;
				contentKey.LoadTimeValidate(encryptionIsUsed);

				// Start loading the data.
				var keyId = Guid.Parse(contentKey.KeyId);

				if (encryptionIsUsed)
				{
					var calculatedMac = mac.ComputeHash(contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue);

					if (!calculatedMac.SequenceEqual(contentKey.Data.Secret.ValueMAC))
						throw new SecurityException("MAC validation failed - the content key value has been tampered with!");

					var iv = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Take(128 / 8).ToArray();
					var encryptedKey = contentKey.Data.Secret.EncryptedValue.CipherData.CipherValue.Skip(128 / 8).ToArray();

					aes.IV = iv;

					byte[] key;

					using (var decryptor = aes.CreateDecryptor())
					{
						key = decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
					}

					cpix.Keys.Add(new ContentKey
					{
						Id = keyId,
						Value = key
					});
				}
				else
				{
					cpix.Keys.Add(new ContentKey
					{
						Id = keyId,
						Value = contentKey.Data.Secret.PlainValue
					});
				}
			}

			if (cpix.Keys.Count == 0)
				throw new NotSupportedException("There were no content keys in the CPIX document.");


			return cpix;
		}

		private static T XmlElementToXmlDeserialized<T>(XmlElement element)
		{
			using (var buffer = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(buffer))
					element.WriteTo(writer);

				buffer.Position = 0;

				var serializer = new XmlSerializer(typeof(T));
				return (T)serializer.Deserialize(buffer);
			}
		}

		/// <summary>
		/// Saves the CPIX document to a stream.
		/// </summary>
		public void Save(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			ValidateState();

			var document = new DocumentRootElement();

			// If null, no encryption is used. when serializing content key elements 
			// If non-null, encryption is used when serializing content key elements.
			AesManaged aes = null;
			HMACSHA512 mac = null;

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

				// 512-bit HMAC key is desirable.
				var macKey = new byte[512 / 8];
				_random.GetBytes(macKey);

				mac = new HMACSHA512(macKey);

				// Generate delivery data for each recipient.
				foreach (var recipient in Recipients.Where(r => r != null))
				{
					var recipientRsa = ((RSACryptoServiceProvider)recipient.PublicKey.Key);

					var encryptedDocumentKey = recipientRsa.Encrypt(documentKey, true);
					var encryptedMacKey = recipientRsa.Encrypt(macKey, true);

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
						},
						MacKey = new MacKey
						{
							Algorithm = Constants.HmacSha512Algorithm,
							Key = new EncryptedXmlValue
							{
								EncryptionMethod = new EncryptionMethodDeclaration
								{
									Algorithm = Constants.RsaOaepAlgorithm
								},
								CipherData = new CipherDataContainer
								{
									CipherValue = encryptedMacKey
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

						// NB! We prepend the IV to the value when saving an encrypted value to the document field.
						var fieldValue = iv.Concat(encryptedValue).ToArray();

						element.Data.Secret.EncryptedValue = new EncryptedXmlValue
						{
							CipherData = new CipherDataContainer
							{
								CipherValue = fieldValue
							},
							EncryptionMethod = new EncryptionMethodDeclaration
							{
								Algorithm = Constants.Aes256CbcAlgorithm
							}
						};

						// Never not MAC.
						element.Data.Secret.ValueMAC = mac.ComputeHash(fieldValue);
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
