using Axinom.Cpix;
using System;
using Xunit;

namespace Tests
{
	/// <summary>
	/// Here we test the interactions of digital signatures and collection modification.
	/// </summary>
	public sealed class SigningInteractionTests
	{
		[Fact]
		public void RemoveEntity_WithRemovedCollectionSignature_Succeeds()
		{
			Execute_RemoveEntity_WithRemovedCollectionSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithRemovedCollectionSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithReappliedCollectionSignature_Succeeds()
		{
			Execute_RemoveEntity_WithReappliedCollectionSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithReappliedCollectionSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithRemoveDocumentSignature_Succeeds()
		{
			Execute_RemoveEntity_WithRemovedDocumentSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithRemovedDocumentSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithReappliedDocumentSignature_Succeeds()
		{
			Execute_RemoveEntity_WithReappliedDocumentSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithReappliedDocumentSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithReappliedDocumentAndCollectionSignature_Succeeds()
		{
			Execute_RemoveEntity_WithReappliedDocumentAndCollectionSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithReappliedDocumentAndCollectionSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithExistingCollectionSignature_Fails()
		{
			Execute_RemoveEntity_WithExistingCollectionSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithExistingCollectionSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithExistingDocumentSignature_Fails()
		{
			Execute_RemoveEntity_WithExistingDocumentSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithExistingDocumentSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		[Fact]
		public void RemoveEntity_WithExistingDocumentAndCollectionSignature_Fails()
		{
			Execute_RemoveEntity_WithExistingDocumentSignature(doc => doc.ContentKeys, doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey()), doc => doc.ContentKeys.Clear());
			Execute_RemoveEntity_WithExistingDocumentSignature(doc => doc.Recipients, doc => doc.Recipients.Add(new Recipient(TestHelpers.PublicRecipient1)), doc => doc.Recipients.Clear());
		}

		private static void Execute_RemoveEntity_WithRemovedCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			collection.RemoveAllSignatures();

			removeEntity(document);
		}

		private static void Execute_RemoveEntity_WithReappliedCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			collection.RemoveAllSignatures();
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			removeEntity(document);
		}

		private static void Execute_RemoveEntity_WithRemovedDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			document.SignedBy = null;

			removeEntity(document);
		}

		private static void Execute_RemoveEntity_WithReappliedDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			document.SignedBy = TestHelpers.PrivateAuthor1;

			removeEntity(document);
		}

		private static void Execute_RemoveEntity_WithReappliedDocumentAndCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			document.SignedBy = TestHelpers.PrivateAuthor1;
			collection.RemoveAllSignatures();
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			removeEntity(document);
		}

		private static void Execute_RemoveEntity_WithExistingCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => removeEntity(document));
		}

		private static void Execute_RemoveEntity_WithExistingDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => removeEntity(document));
		}

		private static void Execute_RemoveEntity_WithExistingDocumentAndCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity, Action<CpixDocument> removeEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.PrivateAuthor1;
			collection.AddSignature(TestHelpers.PrivateAuthor1);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => removeEntity(document));
		}
	}
}
