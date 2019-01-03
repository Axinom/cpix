using Axinom.Cpix.Tests;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Axinom.Cpix.TestVectorGenerator
{
	class Program
	{
		const string OutputDirectoryName = "Output";

		static void Main(string[] args)
		{
			var workingDirectory = Path.Combine(Environment.CurrentDirectory, OutputDirectoryName);

			if (Directory.Exists(workingDirectory))
				Directory.Delete(workingDirectory, true);

			Directory.CreateDirectory(workingDirectory);

			var implementationTypes = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => typeof(ITestVector).IsAssignableFrom(t))
				.Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
				.ToArray();

			Console.WriteLine($"Found {implementationTypes.Length} test vector implementations.");

			using (var readme = new StreamWriter(Path.Combine(workingDirectory, "Readme.md")))
			{
				readme.WriteLine($@"CPIX Test Vectors
=================

Generated {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")} using Axinom.Cpix v{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}

Included test certificates generated as follows: `makecert.exe -pe -n ""CN=CPIX Example Entity 1"" -sky exchange -a sha512 -len 4096 -r -ss My`. The password for any included PFX files is the filename, without extension, case-sensitive.

Test vector descriptions follow below.
");

				foreach (var implementationType in implementationTypes)
				{
					var name = implementationType.Name;
					var instance = (ITestVector)Activator.CreateInstance(implementationType);

					Console.WriteLine("Generating: " + name);

					readme.WriteLine(name);
					readme.WriteLine(new string('=', name.Length));
					readme.WriteLine();

					if (!instance.OutputIsValid)
					{
						readme.WriteLine("NB! This test vector intentionally contains invalid data!");
						readme.WriteLine();
					}

					readme.WriteLine(instance.Description);
					readme.WriteLine();

					using (var file = File.Create(Path.Combine(workingDirectory, name + ".xml")))
					{
						instance.Generate(file);

						if (file.Length == 0)
							throw new Exception("Test vector implementation failed to generate output.");

						if (instance.OutputIsValid != IsValidCpix(file))
							throw new Exception("CPIX validity and our expectation do not match!");
					}
				}
			}

			Console.WriteLine("Copying certificate files to output directory.");

			foreach (var pfx in Directory.GetFiles(Environment.CurrentDirectory, "*.pfx"))
				File.Copy(pfx, Path.Combine(workingDirectory, Path.GetFileName(pfx)));

			foreach (var cer in Directory.GetFiles(Environment.CurrentDirectory, "*.cer"))
				File.Copy(cer, Path.Combine(workingDirectory, Path.GetFileName(cer)));

			Console.WriteLine("All done. Generated output is at " + workingDirectory);
		}

		static bool IsValidCpix(Stream stream)
		{
			stream.Position = 0;

			try
			{
				CpixDocument.Load(stream, new[]
				{
					TestHelpers.Certificate1WithPrivateKey,
					TestHelpers.Certificate2WithPrivateKey,
					TestHelpers.Certificate3WithPrivateKey,
					TestHelpers.Certificate4WithPrivateKey,
				});

				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
