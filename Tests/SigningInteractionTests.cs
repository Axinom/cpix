using System;
using System.Linq;
using Xunit;

namespace Axinom.Cpix.Tests
{
	/// <summary>
	/// Here we test the interactions of digital signatures and collection modification.
	/// </summary>
	public sealed class SigningInteractionTests
	{
		private Func<CpixDocument, EntityCollectionBase> RecipientsSelector = doc => doc.Recipients;
		private Func<CpixDocument, EntityCollectionBase> ContentKeysSelector = doc => doc.ContentKeys;
		private Func<CpixDocument, EntityCollectionBase> DrmSystemsSelector = doc => doc.DrmSystems;
		private Func<CpixDocument, EntityCollectionBase> ContentKeyPeriodsSelector = doc => doc.ContentKeyPeriods;
		private Func<CpixDocument, EntityCollectionBase> UsageRulesSelector = doc => doc.UsageRules;

		private Action<CpixDocument> AddRecipient = doc => doc.Recipients.Add(new Recipient(TestHelpers.Certificate3WithPublicKey));
		private Action<CpixDocument> AddContentKey = doc => doc.ContentKeys.Add(TestHelpers.GenerateContentKey());
		private Action<CpixDocument> AddDrmSystem = doc =>
		{
			doc.ContentKeys.Add(TestHelpers.GenerateContentKey());
			doc.DrmSystems.Add(new DrmSystem
			{
				SystemId = DrmSignalingHelpers.PlayReadySystemId,
				KeyId = doc.ContentKeys.First().Id,
				ContentProtectionData = DrmSignalingHelpers.GeneratePlayReadyDashSignaling(doc.ContentKeys.First().Id),

			});
		};
		private Action<CpixDocument> AddContentKeyPeriod = doc =>
		{
			doc.ContentKeyPeriods.Add(new ContentKeyPeriod { Index = 1 });
		};
		private Action<CpixDocument> AddUsageRule = doc =>
		{
			doc.ContentKeys.Add(TestHelpers.GenerateContentKey());
			doc.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "keyperiod_" + Guid.NewGuid(), Index = 1 });
			TestHelpers.AddUsageRule(doc);
		};


		[Fact]
		public void Clear_WithRemovedCollectionSignature_Succeeds()
		{
			Execute_Clear_WithRemovedCollectionSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithRemovedCollectionSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithRemovedCollectionSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithRemovedCollectionSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithRemovedCollectionSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithReappliedCollectionSignature_Succeeds()
		{
			Execute_Clear_WithReappliedCollectionSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithReappliedCollectionSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithReappliedCollectionSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithReappliedCollectionSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithReappliedCollectionSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithRemoveDocumentSignature_Succeeds()
		{
			Execute_Clear_WithRemovedDocumentSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithRemovedDocumentSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithRemovedDocumentSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithRemovedDocumentSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithRemovedDocumentSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithReappliedDocumentSignature_Succeeds()
		{
			Execute_Clear_WithReappliedDocumentSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithReappliedDocumentSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithReappliedDocumentSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithReappliedDocumentSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithReappliedDocumentSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithReappliedDocumentAndCollectionSignature_Succeeds()
		{
			Execute_Clear_WithReappliedDocumentAndCollectionSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithReappliedDocumentAndCollectionSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithReappliedDocumentAndCollectionSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithReappliedDocumentAndCollectionSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithReappliedDocumentAndCollectionSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithExistingCollectionSignature_Fails()
		{
			Execute_Clear_WithExistingCollectionSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithExistingCollectionSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithExistingCollectionSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithExistingCollectionSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithExistingCollectionSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithExistingDocumentSignature_Fails()
		{
			Execute_Clear_WithExistingDocumentSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithExistingDocumentSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithExistingDocumentSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithExistingDocumentSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithExistingDocumentSignature(UsageRulesSelector, AddUsageRule);
		}

		[Fact]
		public void Clear_WithExistingDocumentAndCollectionSignature_Fails()
		{
			Execute_Clear_WithExistingDocumentAndCollectionSignature(RecipientsSelector, AddRecipient);
			Execute_Clear_WithExistingDocumentAndCollectionSignature(ContentKeysSelector, AddContentKey);
			Execute_Clear_WithExistingDocumentAndCollectionSignature(DrmSystemsSelector, AddDrmSystem);
			Execute_Clear_WithExistingDocumentAndCollectionSignature(ContentKeyPeriodsSelector, AddContentKeyPeriod);
			Execute_Clear_WithExistingDocumentAndCollectionSignature(UsageRulesSelector, AddUsageRule);
		}

		private static void Execute_Clear_WithRemovedCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			collection.RemoveAllSignatures();

			collection.Clear();
		}

		private static void Execute_Clear_WithReappliedCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			collection.RemoveAllSignatures();
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			collection.Clear();
		}

		private static void Execute_Clear_WithRemovedDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			addEntity(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			document = TestHelpers.Reload(document);
			var collection = collectionSelector(document);

			document.SignedBy = null;

			collection.Clear();
		}

		private static void Execute_Clear_WithReappliedDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			addEntity(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			document = TestHelpers.Reload(document);
			var collection = collectionSelector(document);

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			collection.Clear();
		}

		private static void Execute_Clear_WithReappliedDocumentAndCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;
			collection.RemoveAllSignatures();
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			collection.Clear();
		}

		private static void Execute_Clear_WithExistingCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => collection.Clear());
		}

		private static void Execute_Clear_WithExistingDocumentSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			addEntity(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;

			document = TestHelpers.Reload(document);
			var collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => collection.Clear());
		}

		private static void Execute_Clear_WithExistingDocumentAndCollectionSignature(Func<CpixDocument, EntityCollectionBase> collectionSelector, Action<CpixDocument> addEntity)
		{
			var document = new CpixDocument();
			var collection = collectionSelector(document);

			addEntity(document);
			document.SignedBy = TestHelpers.Certificate1WithPrivateKey;
			collection.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);
			collection = collectionSelector(document);

			Assert.Throws<InvalidOperationException>(() => collection.Clear());
		}
	}
}
