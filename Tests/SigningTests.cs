using Axinom.Cpix;
using System;
using System.Linq;
using Xunit;

namespace Tests
{
	public sealed class SigningTests
	{
		[Fact]
		public void AddingContentKeySignature_ToNewDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.AddContentKeySignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.ContentKeysSignedBy.Count);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeysSignedBy.Single().Thumbprint);
		}

		[Fact]
		public void AddingContentKeySignature_ToLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document = TestHelpers.Reload(document);

			// It would be strange to sign content keys when you are not actually the one adding them to the
			// document on initial creation, so we outlaw this purely on grounds of security model sensibility.
			Assert.Throws<InvalidOperationException>(() => document.AddContentKeySignature(TestHelpers.PrivateAuthor1));
		}

		[Fact]
		public void AddingDocumentSignature_ToNewDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.DocumentSignedBy);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void AddingDocumentSignature_ToLoadedPreviouslyUnsignedDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);
			document = TestHelpers.Reload(document);

			Assert.NotNull(document.DocumentSignedBy);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void ReplacingDocumentSignature_OnLoadedPreviouslySignedDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.SetDocumentSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.DocumentSignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor2.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void ReplacingDocumentSignature_WithNewSignatureFromSameIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			// We modify some data to verify that the above actually "did" something.
			document.AddUsageRule(new UsageRule
			{
				KeyId = document.ContentKeys.Single().Id
			});

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.DocumentSignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void RemovingDocumentSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.SetDocumentSignature(null);

			document = TestHelpers.Reload(document);

			Assert.Null(document.DocumentSignedBy);
		}

		[Fact]
		public void LoadingDocument_WithTwiceSignedContentKeys_DetectsBothSignatures()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			document.AddContentKeySignature(TestHelpers.PrivateAuthor1);
			document.AddContentKeySignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Null(document.DocumentSignedBy);
			Assert.Equal(2, document.ContentKeysSignedBy.Count);

			Assert.Equal(1, document.ContentKeysSignedBy.Count(signer => signer.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
			Assert.Equal(1, document.ContentKeysSignedBy.Count(signer => signer.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
		}

		[Fact]
		public void LoadingDocument_WithSignedEverything_DetectsAllSignatures()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);
			document.AddContentKeySignature(TestHelpers.PrivateAuthor1);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.ContentKeysSignedBy.Count);
			Assert.NotNull(document.DocumentSignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.UsageRulesSignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeysSignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void AddSignature_WithoutPrivateKey_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);

			Assert.ThrowsAny<ArgumentException>(() => document.AddContentKeySignature(TestHelpers.PublicAuthor1));
			Assert.ThrowsAny<ArgumentException>(() => document.SetDocumentSignature(TestHelpers.PublicAuthor1));
			Assert.ThrowsAny<ArgumentException>(() => document.AddUsageRuleSignature(TestHelpers.PublicAuthor1));
		}

		[Fact]
		public void AddUsageRuleSignature_WithNoUsageRules_Fails()
		{
			var document = new CpixDocument();
			Assert.Throws<InvalidOperationException>(() => document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1));
		}

		[Fact]
		public void AddUsageRuleSignature_WithSignedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}

		[Fact]
		public void AddUsageRuleSignature_WithExistingUsageRuleSignatures_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.UsageRulesSignedBy.Count);
			Assert.Equal(1, document.UsageRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
			Assert.Equal(1, document.UsageRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}

		[Fact]
		public void ResignUsageRules_WithSameIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveUsageRuleSignatures();
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRulesSignedBy.Count);
			Assert.Equal(1, document.UsageRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
		}

		[Fact]
		public void ResignUsageRules_WithDifferentIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveUsageRuleSignatures();
			document.AddUsageRuleSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRulesSignedBy.Count);
			Assert.Equal(1, document.UsageRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}
	}
}
