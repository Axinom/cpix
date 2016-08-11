using Axinom.Cpix;
using System;
using System.IO;
using Tests;

namespace TestVectorGenerator
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
