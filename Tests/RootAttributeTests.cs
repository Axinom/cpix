using System;
using System.IO;
using System.Xml;
using Axinom.Cpix.Internal;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class RootAttributeTests
	{
		[Fact]
		public void ContentId_ByDefault_IsNull()
		{
			var document = new CpixDocument();
			Assert.Null(document.ContentId);
		}

		[Fact]
		public void ContentId_WhenNull_IsNotSerialized()
		{
			var document = new CpixDocument();
			document.ContentId = null;

			var buffer = new MemoryStream();
			document.Save(buffer);
			buffer.Position = 0;

			var xmlDocument = new XmlDocument();
			xmlDocument.Load(buffer);

			var contentIdAttribute = xmlDocument.DocumentElement.GetAttributeNode(DocumentRootElement.ContentIdAttributeName);

			Assert.Null(contentIdAttribute);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("hello")]
		public void ContentId_WhenSetToVariousValues_SurvivesRoundtrip(string expectedContentId)
		{
			var document = new CpixDocument();
			document.ContentId = expectedContentId;
			document = TestHelpers.Reload(document);

			Assert.NotNull(document.ContentKeys);
			Assert.Equal(expectedContentId, document.ContentId);
		}

		[Fact]
		public void ContentId_WhenSetWhileDocumentIsReadOnly_ThrowsInvalidOperationException()
		{
			var document = new CpixDocument();
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;
			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => document.ContentId = "fail");
		}
	}
}
