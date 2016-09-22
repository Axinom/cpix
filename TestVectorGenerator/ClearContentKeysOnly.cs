using Axinom.Cpix.Tests;
using System.IO;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class ClearContentKeysOnly : ITestVector
	{
		public string Description => "Content keys with values in the clear (without encryption).";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.Save(outputStream);
		}
	}
}
