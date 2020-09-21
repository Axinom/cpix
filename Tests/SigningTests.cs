using System;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class SigningTests
	{
		[Fact]
		public void AddingSignatures_ToEverythingInEmptyNewDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void AddingSignatures_ToEverythingInEmptyLoadedDocument_Succeeds()
		{
			var document = new CpixDocument();
			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void AddingSignature_OnEverythingInAlreadySignedEmptyDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			// Mark the entire document for re-signing, making it editable.
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}
		}

		[Fact]
		public void ReplacingSignature_OnEverythingInEmptyDocument_Succeeds()
		{
			var document = new CpixDocument();
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.Certificate2WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.RemoveAllSignatures();
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Single(collection.SignedBy);
				Assert.Equal(TestHelpers.Certificate2WithPrivateKey.Thumbprint, collection.SignedBy.Single().Thumbprint);
			}
			
			Assert.Equal(TestHelpers.Certificate2WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void AddingSignatures_ToEverythingInFullyPopulatedNewDocument_Succeeds()
		{
			var document = new CpixDocument();
			TestHelpers.PopulateCollections(document);
			document.ContentId = "test";

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void AddingSignatures_ToEverythingInFullyPopulatedLoadedDocument_Succeeds()
		{
			var document = new CpixDocument();
			TestHelpers.PopulateCollections(document);
			document.ContentId = "test";

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}

			Assert.Equal(TestHelpers.Certificate1WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void AddingSignature_OnEverythingInAlreadySignedFullyPopulatedDocument_Succeeds()
		{
			var document = new CpixDocument();
			TestHelpers.PopulateCollections(document);
			document.ContentId = "test";

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			// Mark the entire document for re-signing, making it editable.
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Equal(2, collection.SignedBy.Count());

				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate1WithPrivateKey.Thumbprint);
				Assert.Contains(collection.SignedBy, c => c.Thumbprint == TestHelpers.Certificate2WithPrivateKey.Thumbprint);
			}
		}

		[Fact]
		public void ReplacingSignature_OnEverythingInFullyPopulatedDocument_Succeeds()
		{
			var document = new CpixDocument();
			TestHelpers.PopulateCollections(document);
			document.ContentId = "test";

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			foreach (var collection in document.EntityCollections)
				collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			document.SignedBy = TestHelpers.Certificate2WithPrivateKey;

			foreach (var collection in document.EntityCollections)
			{
				collection.RemoveAllSignatures();
				collection.AddSignature(TestHelpers.Certificate2WithPrivateKey);
			}

			document = TestHelpers.Reload(document);

			foreach (var collection in document.EntityCollections)
			{
				Assert.Single(collection.SignedBy);
				Assert.Equal(TestHelpers.Certificate2WithPrivateKey.Thumbprint, collection.SignedBy.Single().Thumbprint);
			}

			Assert.Equal(TestHelpers.Certificate2WithPrivateKey.Thumbprint, document.SignedBy?.Thumbprint);
		}

		[Fact]
		public void RemovingDocumentSignature_Succeeds()
		{
			var document = new CpixDocument();
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			document = TestHelpers.Reload(document);

			Assert.NotNull(document.SignedBy);
			document.SignedBy = null;

			document = TestHelpers.Reload(document);

			Assert.Null(document.SignedBy);
		}

		[Fact]
		public void AddSignature_WithoutPrivateKey_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period_1", Index = 1});
			TestHelpers.AddUsageRule(document);

			Assert.ThrowsAny<ArgumentException>(() => document.ContentKeys.AddSignature(TestHelpers.Certificate1WithPublicKey));
			Assert.ThrowsAny<ArgumentException>(() => document.SignedBy = TestHelpers.Certificate1WithPublicKey);
			Assert.ThrowsAny<ArgumentException>(() => document.UsageRules.AddSignature(TestHelpers.Certificate1WithPublicKey));
		}
		
		[Fact]
		public void AddUsageRuleSignature_WithSignedDocument_Fails()
		{
			var document = new CpixDocument();
			document.ContentKeys.Add(TestHelpers.GenerateContentKey());
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "period_1", Index = 1 });
			TestHelpers.AddUsageRule(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			document = TestHelpers.Reload(document);

			Assert.Throws<InvalidOperationException>(() => TestHelpers.AddUsageRule(document));
		}
	}
}
