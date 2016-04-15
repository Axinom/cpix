using Axinom.Cpix;
using System;
using System.IO;
using Xunit;

namespace Tests
{
	public sealed class ContentKeyTests
	{
		[Fact]
		public void Save_WithNoKeys_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidOperationException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddContentKey_WithLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => document.AddContentKey(TestHelpers.GenerateContentKey()));
		}

		[Fact]
		public void AddContentKey_WithVariousInvalidData_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.AddContentKey(new ContentKey
			{
				Id = Guid.Empty,
				Value = new byte[16]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddContentKey(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[15]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddContentKey(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[17]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddContentKey(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[0]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.AddContentKey(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = null
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedContentKey_Fails()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.AddContentKey(contentKey);

			// Corrupt it!
			contentKey.Value = null;

			Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
		}
	}
}
