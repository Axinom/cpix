using Axinom.Cpix;
using System;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class EncryptionTests
	{
		[Fact]
		public void LoadingEncryptedKey_WithRecipientPrivateKey_DecryptsKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.AddContentKey(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.AddRecipient(TestHelpers.PublicRecipient1);

			document = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient1 });

			var key = document.ContentKeys.Single();
			Assert.NotNull(key.Value);
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
			Assert.True(document.ContentKeysAvailable);
		}

		[Fact]
		public void LoadingEncryptedKey_WithoutRecipientPrivateKey_SucceedsWithoutDecryptingKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.AddContentKey(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.AddRecipient(TestHelpers.PublicRecipient1);

			document = TestHelpers.Reload(document);

			var key = document.ContentKeys.Single();
			Assert.Null(key.Value);
			Assert.Equal(keyData.Item1, key.Id);
			Assert.False(document.ContentKeysAvailable);
		}

		[Fact]
		public void LoadingDocument_WithTwoRecipients_DetectsRecipients()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());

			document.AddRecipient(TestHelpers.PublicRecipient1);
			document.AddRecipient(TestHelpers.PublicRecipient2);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.Recipients.Count);

			Assert.Equal(1, document.Recipients.Count(r => r.Thumbprint == TestHelpers.PublicRecipient1.Thumbprint));
			Assert.Equal(1, document.Recipients.Count(r => r.Thumbprint == TestHelpers.PublicRecipient2.Thumbprint));
		}

		[Fact]
		public void AddRecipient_WithLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			// Conceivably, one might wish to relax it so you can add recipients if you have access to the document key
			// but there does not appear to be any real world scenario for that, so it seems pointless to consider here.
			Assert.Throws<InvalidOperationException>(() => document.AddRecipient(TestHelpers.PublicRecipient1));
		}
	}
}
