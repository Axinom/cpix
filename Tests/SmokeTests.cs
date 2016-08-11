using Axinom.Cpix;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class SmokeTests
	{
		[Fact]
		public void NewDocument_CreatesEmptyDocument()
		{
			var document = new CpixDocument();

			Assert.True(document.ContentKeysAreReadable);
			Assert.False(document.IsReadOnly);
			Assert.Null(document.SignedBy);
			Assert.Equal(0, document.Recipients.Count);
			Assert.Equal(0, document.ContentKeys.Count);
			Assert.Equal(0, document.UsageRules.Count);
		}

		[Fact]
		public void SaveAndLoad_EmptyDocument_Succeeds()
		{
			var document = new CpixDocument();

			document = TestHelpers.Reload(document);

			Assert.True(document.ContentKeysAreReadable);
			Assert.False(document.IsReadOnly);
			Assert.Null(document.SignedBy);
			Assert.Equal(0, document.Recipients.Count);
			Assert.Equal(0, document.ContentKeys.Count);
			Assert.Equal(0, document.UsageRules.Count);
		}

		[Fact]
		public void Save_OneClearKey_DoesNotHorriblyFail()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			using (var buffer = new MemoryStream())
			{
				document.Save(buffer);

				// Something got saved and there was no exception. Good enough!
				Assert.NotEqual(0, buffer.Length);
			}
		}

		[Fact]
		public void Save_OneEncryptedKey_DoesNotHorriblyFail()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

			using (var buffer = new MemoryStream())
			{
				document.Save(buffer);

				// Something got saved and there was no exception. Good enough!
				Assert.NotEqual(0, buffer.Length);
			}
		}

		[Fact]
		public void RoundTrip_WithOneClearKey_LoadsExpectedKey()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
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
		public void RoundTrip_WithOneKeyEncryptedAndSigned_LoadsExpectedKeyAndDetectsIdentities()
		{
			var keyData = TestHelpers.GenerateKeyData();

			var document = new CpixDocument();
			document.ContentKeys.Add(new ContentKey
			{
				Id = keyData.Item1,
				Value = keyData.Item2
			});

			document.ContentKeys.AddSignature(TestHelpers.Certificate1WithPrivateKey);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;
			document.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));

			document = TestHelpers.Reload(document, new[] { TestHelpers.Certificate3WithPrivateKey });

			Assert.Equal(1, document.ContentKeys.Count);
			Assert.NotNull(document.SignedBy);
			Assert.Equal(1, document.ContentKeys.SignedBy.Count());
			Assert.Equal(1, document.Recipients.Count);

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy.Thumbprint);
			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.ContentKeys.SignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.Certificate3WithPrivateKey.Thumbprint, document.Recipients.Single().Certificate.Thumbprint);

			var key = document.ContentKeys.Single();
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
		}
	}
}
