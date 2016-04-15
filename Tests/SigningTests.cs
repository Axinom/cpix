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
			document.AddAssignmentRule(new AssignmentRule
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
			TestHelpers.AddAssignmentRule(document);
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);
			document.AddContentKeySignature(TestHelpers.PrivateAuthor1);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.ContentKeysSignedBy.Count);
			Assert.NotNull(document.DocumentSignedBy);

			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.AssignmentRulesSignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.ContentKeysSignedBy.Single().Thumbprint);
			Assert.Equal(TestHelpers.PrivateAuthor1.Thumbprint, document.DocumentSignedBy.Thumbprint);
		}

		[Fact]
		public void AddSignature_WithoutPrivateKey_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);

			Assert.ThrowsAny<ArgumentException>(() => document.AddContentKeySignature(TestHelpers.PublicAuthor1));
			Assert.ThrowsAny<ArgumentException>(() => document.SetDocumentSignature(TestHelpers.PublicAuthor1));
			Assert.ThrowsAny<ArgumentException>(() => document.AddAssignmentRuleSignature(TestHelpers.PublicAuthor1));
		}

		[Fact]
		public void AddAssignmentRuleSignature_WithNoAssignmentRules_Fails()
		{
			var document = new CpixDocument();
			Assert.Throws<InvalidOperationException>(() => document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1));
		}

		[Fact]
		public void AddAssignmentRuleSignature_WithSignedDocument_Fails()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.SetDocumentSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddAssignmentRule(document));
		}

		[Fact]
		public void AddAssignmentRuleSignature_WithExistingAssignmentRuleSignatures_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.AssignmentRulesSignedBy.Count);
			Assert.Equal(1, document.AssignmentRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
			Assert.Equal(1, document.AssignmentRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}

		[Fact]
		public void ResignAssignmentRules_WithSameIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveAssignmentRuleSignatures();
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.AssignmentRulesSignedBy.Count);
			Assert.Equal(1, document.AssignmentRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor1.Thumbprint));
		}

		[Fact]
		public void ResignAssignmentRules_WithDifferentIdentity_Succeeds()
		{
			var document = new CpixDocument();
			document.AddContentKey(TestHelpers.GenerateContentKey());
			TestHelpers.AddAssignmentRule(document);
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);

			document.RemoveAssignmentRuleSignatures();
			document.AddAssignmentRuleSignature(TestHelpers.PrivateAuthor2);

			document = TestHelpers.Reload(document);

			Assert.Equal(1, document.AssignmentRulesSignedBy.Count);
			Assert.Equal(1, document.AssignmentRulesSignedBy.Count(c => c.Thumbprint == TestHelpers.PrivateAuthor2.Thumbprint));
		}
	}
}
