using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Axinom.Cpix
{
	public sealed class Document
	{
		/// <summary>
		/// Whether the values of content keys are available.
		/// 
		/// This is always true for new documents. This may be false for loaded documents if the content keys
		/// were encrypted and we do not possess any of the delivery keys.
		/// </summary>
		public bool ContentKeysAvailable { get; private set; } = true;

		/// <summary>
		/// Gets whether the document is read-only.
		/// 
		/// This can be the case if you are dealing with a loaded CPIX document that contains a signature that includes the
		/// entire document in scope. You must remove or re-apply the signature to make the document writable.
		/// </summary>
		public bool IsReadOnly { get { throw new NotImplementedException(); } }

		/// <summary>
		/// Certificate of the identity that has signed the document as a whole.
		/// </summary>
		public X509Certificate2 SignedBy { get; }

		/// <summary>
		/// Creates, recreates or removes the signature over the entire document.
		/// Set to null to remove the document signature or to a certificate to add/replace one.
		/// </summary>
		public void SetDocumentSignature(X509Certificate2 signingCertificate)
		{
			/*if (signingCertificate != null)
				ValidateSigningCertificate(signingCertificate);

			if (_loadedDocumentSignature != null)
			{
				_loadedDocumentSignature.ParentNode.RemoveChild(_loadedDocumentSignature);
				_loadedDocumentSignature = null;

				// Signals that old signature is no longer meaningful. Whatever is in "desired" counts.
				_loadedDocumentSigner = null;
			}

			_desiredDocumentSigner = signingCertificate;*/
		}

		/// <summary>
		/// Throws an exception if the document is read-only.
		/// </summary>
		internal void VerifyIsNotReadOnly()
		{
			if (!IsReadOnly)
				return;

			throw new InvalidOperationException("The document is read-only. You must remove or re-apply any digital signatures on the document to make it writable.");
		}
	}
}
