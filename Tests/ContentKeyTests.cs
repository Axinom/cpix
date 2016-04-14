using Axinom.Cpix;
using System;
using System.IO;
using Xunit;

namespace Tests
{
	public sealed class ContentKeyTests
	{
		[Fact]
		public void SavingDocument_WithNoKeys_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidOperationException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddingKey_ToLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => document.AddContentKey(TestHelpers.GenerateContentKey()));
		}
	}
}
