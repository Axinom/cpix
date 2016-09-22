using System;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
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

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

			document = TestHelpers.Reload(document, new[] { TestHelpers.Certificate3WithPrivateKey });

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

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

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

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.Recipients.Count);

			Assert.Equal(1, document.Recipients.Count(r => r.Certificate.Thumbprint == TestHelpers.Certificate3WithPublicKey.Thumbprint));
			Assert.Equal(1, document.Recipients.Count(r => r.Certificate.Thumbprint == TestHelpers.Certificate4WithPublicKey.Thumbprint));
		}

		[Fact]
		public void LoadingDocument_WithTwoRecipients_DecryptsContentKeysWithEitherRecipient()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
			document.Recipients.Add(new Recipient(TestHelpers.Certificate4WithPublicKey));

			var decryptedDocument1 = TestHelpers.Reload(document, new[] { TestHelpers.Certificate3WithPrivateKey });
			var decryptedDocument2 = TestHelpers.Reload(document, new[] { TestHelpers.Certificate4WithPrivateKey });

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
			Assert.Throws<InvalidOperationException>(() => document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey)));
		}

		[Fact]
		public void AddRecipient_WithLoadedDocumentAndWrittenContentKeys_SucceedsAndEncryptsContentKeys()
		{
			// We start with some clear keys.
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			// Now we re-add keys to mark them for processing.
			var keys = document.ContentKeys.ToArray();
			document.ContentKeys.Clear();

			foreach (var key in keys)
				document.ContentKeys.Add(key);

			// This marks the keys as to be encrypted.
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

			document = TestHelpers.Reload(document, new[] { TestHelpers.Certificate3WithPrivateKey });

			Assert.Equal(1, document.Recipients.Count);
			Assert.True(document.ContentKeysAreReadable);
		}

		[Fact]
		public void RemoveRecipients_WithLoadedDocumentAndWrittenContentKeys_SucceedsAndDecryptsContentKeys()
		{
			// We start with some encrypted keys.
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

			// Load and decrypt keys.
			document = TestHelpers.Reload(document, new[] { TestHelpers.Certificate3WithPrivateKey });

			// Re-add keys to mark them for processing.
			var keys = document.ContentKeys.ToArray();
			document.ContentKeys.Clear();

			foreach (var key in keys)
				document.ContentKeys.Add(key);

			// Remove recipients - we will output clear keys.
			document.Recipients.Clear();

			document = TestHelpers.Reload(document);

			Assert.Equal(0, document.Recipients.Count);
			Assert.True(document.ContentKeysAreReadable);
		}
	}
}
