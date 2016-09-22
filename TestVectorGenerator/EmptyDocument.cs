using System.IO;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class EmptyDocument : ITestVector
	{
		public string Description => "An empty document. Valid, though rather useless.";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			new CpixDocument().Save(outputStream);
		}
	}
}
