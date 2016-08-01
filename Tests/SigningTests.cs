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
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.ContentKeys.SignedBy.Count());
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeys.SignedBy.Single().Thumbprint);
		}

		[Fact]
		public void AddingContentKeySignature_ToLoadedDocument_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document = TestHelpers.Reload(document);

			// It would be strange to sign content keys when you are not actually the one adding them to the
			// document on initial creation, so we outlaw this purely on grounds of security model sensibility.
			Assert.Throws<InvalidOperationException>(() => document.ContentKeys.AddSignature(TestHelpers.PrivateAuthor1));
		}

		[Fact]
		public void AddingDocumentSignature_ToNewDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.SignedBy);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.SignedBy.Thumbprint);
		}

		[Fact]
		public void AddingDocumentSignature_ToLoadedPreviouslyUnsignedDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.PrivateAuthor1;
			document = TestHelpers.Reload(document);

			Assert.NotNull(document.SignedBy);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.SignedBy.Thumbprint);
		}

		[Fact]
		public void ReplacingDocumentSignature_OnLoadedPreviouslySignedDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.PrivateAuthor2;

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.SignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor2.Thumbprint, document.SignedBy.Thumbprint);
		}

		[Fact]
		public void ReplacingDocumentSignature_WithNewSignatureFromSameIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.PrivateAuthor1;

			// We modify some data to verify that the above actually "did" something.
			document.UsageRules.Add(new UsageRule
			{
				KeyId = document.ContentKeys.Single().Id
			});

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.SignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.SignedBy.Thumbprint);
		}

		[Fact]
		public void RemovingDocumentSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);

			document.SignedBy = null;

			document = TestHelpers.Reload(document);

			Assert.Null(document.SignedBy);
		}

		[Fact]
		public void LoadingDocument_WithTwiceSignedContentKeys_DetectsBothSignatures()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeys.AddSignature(TestHelpers.PrivateAuthor1);
			document.ContentKeys.AddSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Null(document.SignedBy);
			Assert.Equal(2, document.ContentKeys.SignedBy.Count());

			Assert.Equal(1, document.ContentKeys.SignedBy.Count(signer => signer.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
			Assert.Equal(1, document.ContentKeys.SignedBy.Count(signer => signer.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
		}

		[Fact]
		public void LoadingDocument_WithSignedEverything_DetectsAllSignatures()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;
			document.ContentKeys.AddSignature(TestHelpers.PrivateAuthor1);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.ContentKeys.SignedBy.Count());
			Assert.NotNull(document.SignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.UsageRules.SignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeys.SignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.SignedBy.Thumbprint);
		}

		[Fact]
		public void AddSignature_WithoutPrivateKey_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);

			Assert.ThrowsAny<ArgumentException>(() => document.ContentKeys.AddSignature(TestHelpers.PublicAuthor1));
			Assert.ThrowsAny<ArgumentException>(() => document.SignedBy = TestHelpers.PublicAuthor1);
			Assert.ThrowsAny<ArgumentException>(() => document.UsageRules.AddSignature(TestHelpers.PublicAuthor1));
		}

		[Fact]
		public void AddUsageRuleSignature_WithNoUsageRules_Fails()
		{
			var document = new CpixDocument();
			Assert.Throws<InvalidOperationException>(() => document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1));
		}

		[Fact]
		public void AddUsageRuleSignature_WithSignedDocument_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}

		[Fact]
		public void AddUsageRuleSignature_WithExistingUsageRuleSignatures_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.UsageRules.SignedBy.Count());
			Assert.Equal(1, document.UsageRules.SignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
			Assert.Equal(1, document.UsageRules.SignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}

		[Fact]
		public void ResignUsageRules_WithSameIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.UsageRules.RemoveAllSignatures();
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRules.SignedBy.Count());
			Assert.Equal(1, document.UsageRules.SignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
		}

		[Fact]
		public void ResignUsageRules_WithDifferentIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			TestHelpers.AddUsageRule(document);
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.UsageRules.RemoveAllSignatures();
			document.UsageRules.AddSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.UsageRules.SignedBy.Count());
			Assert.Equal(1, document.UsageRules.SignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}
	}
}
