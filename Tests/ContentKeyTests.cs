using Axinom.Cpix;
using System;
using System.IO;
using Xunit;

namespace Tests
{
	public sealed class ContentKeyTests
	{
		[Fact]
		public void Save_WithNoKeys_Succeeds()
		{
			var document = new CpixDocument();

			document.Save(new MemoryStream());
		}

		[Fact]
		public void AddContentKey_WithLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => document.ContentKeys.Add(TestHelpers.GenerateContentKey()));
		}

		[Fact]
		public void AddContentKey_WithVariousInvalidData_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.Empty,
				Value = new byte[16]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[15]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[17]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeys.Add(new ContentKey
			{
				Id = Guid.NewGuid(),
				Value = new byte[0]
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeys.Add(new ContentKey
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
			document.ContentKeys.Add(contentKey);

			// Corrupt it!
			contentKey.Value = null;

			Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddContentKey_Twice_Fails()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);

			Assert.Throws<ArgumentException>(() => document.ContentKeys.Add(contentKey));
		}
	}
}
