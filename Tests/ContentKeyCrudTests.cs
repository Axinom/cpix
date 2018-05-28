using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class ContentKeyCrudTests
	{
		[Fact]
		public void AddContentKey_WithLoadedEmptyDocument_Succeeds()
		{
			var document = new CpixDocument();
			document = TestHelpers.Reload(document);

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document = TestHelpers.Reload(document);

			Assert.Single(document.ContentKeys);
		}

		[Fact]
		public void AddContentKey_WithLoadedDocumentAndExistingContentKey_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document = TestHelpers.Reload(document);

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.ContentKeys.Count);
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
			// It will be validated here.
			document.ContentKeys.Add(contentKey);

			// Corrupt it after validation!
			contentKey.Value = null;

			// The corruption should still be caught.
			Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void AddContentKey_Twice_Fails()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);

			// Same instance.
			Assert.Throws<ArgumentException>(() => document.ContentKeys.Add(contentKey));

			// Same ID but different instance.
			Assert.ThrowsAny<Exception>(() => document.ContentKeys.Add(new ContentKey
			{
				Id = contentKey.Id,
				Value = contentKey.Value
			}));
		}

		[Fact]
		public void RemoveContentKey_WithNewWritableCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.ContentKeys.Remove(contentKey);
		}

		[Fact]
		public void RemoveContentKey_WithLoadedWritableCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);

			document = TestHelpers.Reload(document);

			document.ContentKeys.Remove(document.ContentKeys.Single());
		}

		[Fact]
		public void RemoveContentKey_WithUnknownContentKey_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Remove(contentKey);
		}

		[Fact]
		public void RoundTrip_WithSignedCollection_Succeeds()
		{
			var contentKey = TestHelpers.GenerateContentKey();

			var document = new CpixDocument();
			document.ContentKeys.Add(contentKey);
			document.ContentKeys.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			Assert.Single(document.ContentKeys);
		}
	}
}
