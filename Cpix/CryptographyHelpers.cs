using Axinom.Cpix.Compatibility;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Axinom.Cpix
{
	static class CryptographyHelpers
	{
		internal static void ValidateRecipientCertificateAndPublicKey(X509Certificate2 certificate)
		{
			if (certificate.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			var rsaKey = certificate.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for recipient certificates.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		internal static void ValidateSignerCertificate(X509Certificate2 certificate)
		{
			if (certificate.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			if (!certificate.HasPrivateKey)
				throw new ArgumentException("The private key of the supplied signer certificate is not available .");

			var rsaKey = certificate.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for signer certificates.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		internal static void ValidateRecipientCertificateAndPrivateKey(X509Certificate2 certificate)
		{
			if (certificate.SignatureAlgorithm.Value == Constants.Sha1Oid)
				throw new ArgumentException("Weak certificates (signed using SHA-1) cannot be used with this library.");

			if (!certificate.HasPrivateKey)
				throw new ArgumentException("The private key of the supplied recipient certificate is not available .");

			var rsaKey = certificate.GetRSAPublicKey();

			if (rsaKey == null)
				throw new ArgumentException("Only RSA keys are currently supported for recipient certificates.");

			if (rsaKey.KeySize < Constants.MinimumRsaKeySizeInBits)
				throw new ArgumentException($"The RSA key must be at least {Constants.MinimumRsaKeySizeInBits} bits long.");
		}

		/// <summary>
		/// Signs an XML element referenced by ID and places the signature element under the document root element.
		/// Pass an empty string as the element to sign the entire document.
		/// </summary>
		internal static XmlElement SignXmlElement(XmlDocument document, string elementToSignId, X509Certificate2 signer)
		{
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			if (elementToSignId == null)
				throw new ArgumentNullException(nameof(elementToSignId));

			if (signer == null)
				throw new ArgumentNullException(nameof(signer));

			using (var signingKey = signer.GetRSAPrivateKey())
			{
				var signedXml = new SignedXml(document)
				{
					SigningKey = signingKey
				};

				// Add each content key assignment rule element as a reference to sign.
				var whatToSign = new Reference
				{
					// A nice strong algorithm without known weaknesses that are easily exploitable.
					DigestMethod = Constants.Sha512Algorithm
				};

				if (elementToSignId == "")
				{
					// Sign the document.
					whatToSign.Uri = "";

					// This is needed because the signature is within the signed data.
					whatToSign.AddTransform(new XmlDsigEnvelopedSignatureTransform());
				}
				else
				{
					// Sign one specific element.
					whatToSign.Uri = "#" + elementToSignId;
				}

				signedXml.AddReference(whatToSign);

				// A nice strong algorithm without known weaknesses that are easily exploitable.
				signedXml.SignedInfo.SignatureMethod = RSAPKCS1SHA512SignatureDescription.Name;

				// Canonical XML 1.0 (omit comments); I suppose it works fine, no deep thoughts about this.
				signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl;

				// Signer certificate must be delivered with the signature.
				signedXml.KeyInfo.AddClause(new KeyInfoX509Data(signer));

				// Ready to sign! Let's go!
				signedXml.ComputeSignature();

				// Now stick the Signature element it generated back into the document and we are done.
				var signature = signedXml.GetXml();
				return (XmlElement)document.DocumentElement.AppendChild(document.ImportNode(signature, true));
			}
		}
	}
}
