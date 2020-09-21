using System.IO;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
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
			Assert.Empty(document.Recipients);
			Assert.Empty(document.ContentKeys);
			Assert.Empty(document.UsageRules);
			Assert.Null(document.ContentId);
		}

		[Fact]
		public void SaveAndLoad_EmptyDocument_Succeeds()
		{
			var document = new CpixDocument();

			document = TestHelpers.Reload(document);

			Assert.True(document.ContentKeysAreReadable);
			Assert.False(document.IsReadOnly);
			Assert.Null(document.SignedBy);
			Assert.Empty(document.Recipients);
			Assert.Empty(document.ContentKeys);
			Assert.Empty(document.UsageRules);
			Assert.Null(document.ContentId);
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
		public void Save_DoesNotAddBomToStream()
		{
			var expectedFirstFiveBytes = new [] { (byte)'<', (byte)'?', (byte)'x', (byte)'m', (byte)'l' } ;

			var document = new CpixDocument();

			using (var buffer = new MemoryStream())
			{
				document.Save(buffer);
				
				buffer.Position = 0;
				var firstFiveBytes = new byte[5];
				buffer.Read(firstFiveBytes, 0, 5);

				Assert.Equal(expectedFirstFiveBytes, firstFiveBytes);
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
			Assert.Single(document.ContentKeys);

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

			Assert.Single(document.ContentKeys);
			Assert.NotNull(document.SignedBy);
			Assert.Single(document.ContentKeys.SignedBy);
			Assert.Single(document.Recipients);

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy.Thumbprint);
			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.ContentKeys.SignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.Certificate3WithPrivateKey.Thumbprint, document.Recipients.Single().Certificate.Thumbprint);

			var key = document.ContentKeys.Single();
			Assert.Equal(keyData.Item1, key.Id);
			Assert.Equal(keyData.Item2, key.Value);
		}
	}
}
