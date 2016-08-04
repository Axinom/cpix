using Axinom.Cpix;
using System;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class ContentKeyEncryptionTests
	{
		[Fact]
		public void LoadingEncryptedKey_WithRecipientPrivateKey_DecryptsKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));

			document = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient1 });

			var key = document.ContentKeys.Single();
			Assert.NotNull(key.Value);
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
			Assert.True(document.ContentKeysAreReadable);
		}

		[Fact]
		public void LoadingEncryptedKey_WithoutRecipientPrivateKey_SucceedsWithoutDecryptingKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));

			document = TestHelpers.Reload(document);

			var key = document.ContentKeys.Single();
			Assert.Null(key.Value);
			Assert.Equal(keyData.Item1, key.Id);
			Assert.False(document.ContentKeysAreReadable);
		}

		[Fact]
		public void LoadingDocument_WithTwoRecipients_DetectsRecipients()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));
			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient2));

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.Recipients.Count);

			Assert.Equal(1, document.Recipients.Count(r => r.Certificate.Thumbprint == TestHelpers.PublicRecipient1.Thumbprint));
			Assert.Equal(1, document.Recipients.Count(r => r.Certificate.Thumbprint == TestHelpers.PublicRecipient2.Thumbprint));
		}

		[Fact]
		public void LoadingDocument_WithTwoRecipients_DecryptsContentKeysWithEitherRecipient()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));
			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient2));

			var decryptedDocument1 = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient1 });
			var decryptedDocument2 = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient2 });

			Assert.True(decryptedDocument1.ContentKeysAreReadable);
			Assert.True(decryptedDocument2.ContentKeysAreReadable);
		}

		[Fact]
		public void AddRecipient_WithLoadedDocumentAndReadContentKeys_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			// The keys are read-only so they cannot be encrypted!
			Assert.Throws<InvalidOperationException>(() => document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)));
		}

		[Fact]
		public void AddRecipient_WithLoadedDocumentAndWrittenContentKeys_SucceedsAndDecryptsContentKeys()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			// Re-add keys to mark them for processing. They will be encrypted.
			var keys = document.ContentKeys.ToArray();
			document.ContentKeys.Clear();

			foreach (var key in keys)
				document.ContentKeys.Add(key);

			document.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1));

			document = TestHelpers.Reload(document, new[] { TestHelpers.PrivateRecipient1 });

			Assert.Equal(1, document.Recipients.Count);
			Assert.True(document.ContentKeysAreReadable);
		}
	}
}
