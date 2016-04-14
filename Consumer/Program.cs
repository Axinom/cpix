using Axinom.Cpix;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace Consumer
{
	class Program
	{
		static void Main(string[] args)
		{
			var recipientCertificate1 = new X509Certificate2("Recipient1.pfx", "Recipient1");
			var recipientCertificate2 = new X509Certificate2("Recipient2.pfx", "Recipient2");
			var trustedSignerCertificate = new X509Certificate2("Author1.cer");

			using (var file = File.OpenRead("Cpix.xml"))
			{
				var cpix = CpixDocument.Load(file, new[] { recipientCertificate1 });

				if (cpix.DocumentSignedBy == null || cpix.DocumentSignedBy.Thumbprint != trustedSignerCertificate.Thumbprint)
					throw new SecurityException("The CPIX document is not signed by a trusted entity!");

				Console.WriteLine("Loaded CPIX with {0} content keys.", cpix.ContentKeys.Count);
			}
		}
	}
}
