using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Cpix
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
		}
	}
}
