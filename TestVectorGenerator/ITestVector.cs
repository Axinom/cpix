using System.IO;

namespace TestVectorGenerator
{
	/// <summary>
	/// Marker interface that must be present on all test vectors.
	/// </summary>
	interface ITestVector
	{
		/// <summary>
		/// Human-readable description of the test vector, for the readme file.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Generates the CPIX document output.
		/// </summary>
		void Generate(Stream outputStream);

		/// <summary>
		/// Whether the generated output is expected to be a valid CPIX document.
		/// </summary>
		bool OutputIsValid { get; }
	}
}
