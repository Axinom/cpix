using Axinom.Cpix;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Labeler
{
	class Program
	{
		static void Main(string[] args)
		{
			var myCertificate = new X509Certificate2("Author2.pfx", "Author2");

			CpixDocument document;
			using (var file = File.OpenRead("Cpix.xml"))
				document = CpixDocument.Load(file);

			if (document.UsageRules.Count != 0)
				throw new Exception("This CPIX document already has assignment rules. Will not touch it for fear of conflicts.");

			// We will sign document as a whole ourselves, overwriting any existing signature.
			// This makes everything else modifiable - without this, everything is read-only if it is already signed.
			document.SignedBy = myCertificate;

			// Remove any signatures that cover the rules, as we are about to modify things.
			document.UsageRules.RemoveAllSignatures();

			var labels = new[]
			{
				"Period1-SelectionSet1-SwitchingSet1-Track1",
				"Period1-SelectionSet1-SwitchingSet1-Track2",
				"Period1-SelectionSet1-SwitchingSet1-Track3",
				"Period1-SelectionSet2-SwitchingSet1-Track1",
				"Period1-SelectionSet2-SwitchingSet1-Track2",
				"Period1-SelectionSet2-SwitchingSet2-Track1",
				"Period1-SelectionSet2-SwitchingSet3-Track2",
			};

			var random = new Random();

			foreach (var label in labels)
			{
				document.UsageRules.Add(new UsageRule
				{
					// Just pick a random content key for each label, for sample purposes.
					KeyId = document.ContentKeys.Skip(random.Next(document.ContentKeys.Count)).First().Id,
					LabelFilters = new[]
					{
						new LabelFilter
						{
							Label = label
						}
					}
				});
			}

			// We will then sign the created rules ourselves.
			document.UsageRules.AddSignature(myCertificate);

			using (var file = File.Create("out.xml"))
				document.Save(file);
		}
	}
}
