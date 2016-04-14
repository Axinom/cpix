using Axinom.Cpix;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class SmokeTests
	{
		[Fact]
		public void RoundTrip_WithOneClearKey_LoadsExpectedKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.AddContentKey(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.ContentKeys);
			Assert.Equal(1, document.ContentKeys.Count);

			var key = document.ContentKeys.Single();
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
		}

		[Fact]
		public void RoundTrip_WithOneKeyEncryptedAndSigned_LadsExpectedKeyAndDetectsIdentities()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.AddContentKey(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.AddContentKeySignature(TestHelpers.PrivateAuthor1);
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);
			document.AddRecipient(TestHelpers.PublicRecipient1);

			document = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient1 });

			Assert.Equal(1, document.ContentKeys.Count);
			Assert.NotNull(document.DocumentSignedBy);
			Assert.Equal(1, document.ContentKeysSignedBy.Count);
			Assert.Equal(1, document.Recipients.Count);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeysSignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateRecipient1.Thumbprint, document.Recipients.Single().Thumbprint);

			var key = document.ContentKeys.Single();
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
		}
	}
}
