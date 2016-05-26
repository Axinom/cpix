using Axinom.Cpix;
using System;
using System.IO;
using System.Linq;
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

			var trustedDocumentSignerCertificate = new X509Certificate2("Author1.cer");
			var trustedContentKeySignerCertificate = new X509Certificate2("Author1.cer");
			var trustedRulesSignerCertificate = new X509Certificate2("Author2.cer");

			using (var file = File.OpenRead("Cpix.xml"))
			{
				var cpix = CpixDocument.Load(file, new[] { recipientCertificate1 });

				// Comment there exceptions out for testing with different test files.
				if (cpix.DocumentSignedBy == null || cpix.DocumentSignedBy.Thumbprint != trustedDocumentSignerCertificate.Thumbprint)
				{
					//throw new SecurityException("The CPIX document is not signed by a trusted entity!");
				}

				if (!cpix.ContentKeysSignedBy.Any(c => c.Thumbprint == trustedContentKeySignerCertificate.Thumbprint))
				{
					//throw new SecurityException("The content keys in this CPIX document are not signed by a trusted entity!");
				}

				if (cpix.UsageRules.Count != 0 && !cpix.UsageRulesSignedBy.Any(c => c.Thumbprint == trustedRulesSignerCertificate.Thumbprint))
				{
					throw new SecurityException("The content key assignment rules in this CPIX document are not signed by a trusted entity!");
				}

				Console.WriteLine("Loaded CPIX with {0} content keys and {1} assignment rules.", cpix.ContentKeys.Count, cpix.UsageRules.Count);

				foreach (var key in cpix.ContentKeys)
				{
					Console.Write(key.Id);

					if (key.Value == null)
						Console.WriteLine(": value unavailable (we were not among the recipients)");
					else
						Console.WriteLine(": " + Convert.ToBase64String(key.Value));
				}

				foreach (var rule in cpix.UsageRules)
					Console.WriteLine("A rule for " + rule.KeyId);
			}
		}
	}
}
