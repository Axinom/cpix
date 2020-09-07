using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class ContentKeyPeriodCrudTests
	{
		[Fact]
		public void AddContentKeyPeriod_WithLoadedEmptyDocument_Succeeds()
		{
			var document = new CpixDocument();
			document = TestHelpers.Reload(document);

			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Index = 1 });
			document = TestHelpers.Reload(document);

			Assert.Single(document.ContentKeyPeriods);
		}

		[Fact]
		public void AddContentKeyPeriod_WithLoadedDocumentWithExistingContentKeyPeriod_Succeeds()
		{
			var document = new CpixDocument();
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Index = 1 });
			document = TestHelpers.Reload(document);

			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Index = 2 });
			document = TestHelpers.Reload(document);

			Assert.Equal(2, document.ContentKeyPeriods.Count);
		}

		[Fact]
		public void AddContentKeyPeriod_WithVariousValidData_Succeeds()
		{
			var document = new CpixDocument();

			Assert.Null(Record.Exception(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Index = 1
			})));
			Assert.Null(Record.Exception(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Start = DateTime.Now,
				End = DateTime.Now
			})));
			Assert.Null(Record.Exception(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Id = "test",
				Start = DateTime.Now,
				End = DateTime.Now
			})));
		}

		[Fact]
		public void AddContentKeyPeriod_WithVariousInvalidData_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Start = DateTime.Now
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				End = DateTime.Now
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Index = 1,
				Start = DateTime.Now
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Index = 1,
				End = DateTime.Now
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.ContentKeyPeriods.Add(new ContentKeyPeriod
			{
				Index = 1,
				Start = DateTime.Now,
				End = DateTime.Now
			}));
		}

		[Fact]
		public void Save_WithSneakilyCorruptedContentKeyPeriod_Fails()
		{
			var contentKeyPeriod = new ContentKeyPeriod { Index = 1 };

			var document = new CpixDocument();
			// It will be validated here.
			document.ContentKeyPeriods.Add(contentKeyPeriod);

			// Corrupt it after validation!
			contentKeyPeriod.Index = null;

			// The corruption should still be caught.
			var ex = Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
			Assert.Contains("index or both the start and end time must be specified", ex.Message);
		}

		[Fact]
		public void RemoveContentKeyPeriod_WithNewWritableCollection_Succeeds()
		{
			var contentKeyPeriod = new ContentKeyPeriod { Index = 1 };

			var document = new CpixDocument();
			document.ContentKeyPeriods.Add(contentKeyPeriod);
			document.ContentKeyPeriods.Remove(contentKeyPeriod);

			Assert.Empty(document.ContentKeyPeriods);
		}

		[Fact]
		public void RemoveContentKeyPeriod_WithLoadedWritableCollection_Succeeds()
		{
			var contentKeyPeriod = new ContentKeyPeriod { Index = 1 };

			var document = new CpixDocument();
			document.ContentKeyPeriods.Add(contentKeyPeriod);
			document = TestHelpers.Reload(document);
			document.ContentKeyPeriods.Remove(document.ContentKeyPeriods.Single());

			Assert.Empty(document.ContentKeyPeriods);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("start=\"2020-09-02T18:40:49.4082171\"")]
		[InlineData("end=\"2020-09-02T18:40:49.4082171\"")]
		[InlineData("index=\"1\" start=\"2020-09-02T18:40:49.4082171\"")]
		[InlineData("index=\"1\" end=\"2020-09-02T18:40:49.4082171\"")]
		[InlineData("index=\"1\" start=\"2020-09-02T18:40:49.4082171\" end=\"2020-09-02T18:40:49.4082171\"")]
		public void Load_WithCpixContainingContentKeyPeriodWithInvalidIndexAndStartAndEndCombination_Fails(string invalidContentKeyPeriodAttributeSet)
		{
			const string cpixTemplate = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyPeriodList><ContentKeyPeriod {0} /></ContentKeyPeriodList></CPIX>";

			var cpix = string.Format(cpixTemplate, invalidContentKeyPeriodAttributeSet);

			var ex = Assert.Throws<InvalidCpixDataException>(() => CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(cpix))));
			Assert.Contains("index or both the start and end time must be specified", ex.Message);
		}

		[Fact]
		public void Save_WithCpixContainingInvalidContentKeyPeriodIdValue_FailsSchemaValidation()
		{
			var document = new CpixDocument();
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "1cannotstartwithnumber", Index = 1 });

			var ex = Assert.Throws<XmlSchemaValidationException>(() => document.Save(new MemoryStream()));
			Assert.Contains("invalid according to its datatype", ex.Message);
		}

		[Fact]
		public void Load_WithCpixContainingInvalidContentKeyPeriodIdValue_FailsSchemaValidation()
		{
			var ex = Assert.Throws<XmlSchemaValidationException>(() =>
				CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyPeriodList><ContentKeyPeriod id=\"1cannotstartwithnumber\" index=\"1\" /></ContentKeyPeriodList></CPIX>"))));

			Assert.Contains("invalid according to its datatype", ex.Message);
		}

		[Fact]
		public void Save_WithCpixContainingContentKeyPeriodsWithNonUniqueIds_FailsSchemaValidation()
		{
			var document = new CpixDocument();
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "duplicate", Index = 1 });
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = "duplicate", Index = 2 });

			var ex = Assert.Throws<XmlSchemaValidationException>(() => document.Save(new MemoryStream()));
			Assert.Contains("is already used as an ID", ex.Message);
		}

		[Fact]
		public void Load_WithCpixContainingContentKeyPeriodsWithNonUniqueIds_FailsSchemaValidation()
		{
			var ex = Assert.Throws<XmlSchemaValidationException>(() =>
				CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyPeriodList><ContentKeyPeriod id=\"id1\" index=\"1\" /><ContentKeyPeriod id=\"id1\" index=\"2\" /></ContentKeyPeriodList></CPIX>"))));

			Assert.Contains("is already used as an ID", ex.Message);
		}
		
		[Fact]
		public void Roundtrip_WithSignedCollectionOfVariousValidContentKeyPeriods_Succeeds()
		{
			var document = new CpixDocument();

			var expectedPeriod1Index = 1;
			var expectedPeriod2Id = "period_2";
			var expectedPeriod2Index = 2;
			var expectedPeriod3Id = "period_3";
			var expectedPeriod3Start = DateTime.Now;
			var expectedPeriod3End = DateTime.Now.AddHours(1);

			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Index = expectedPeriod1Index });
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = expectedPeriod2Id, Index = expectedPeriod2Index});
			document.ContentKeyPeriods.Add(new ContentKeyPeriod { Id = expectedPeriod3Id, Start = expectedPeriod3Start, End = expectedPeriod3End });
			document.ContentKeyPeriods.AddSignature(TestHelpers.Certificate1WithPrivateKey);

			document = TestHelpers.Reload(document);

			Assert.Equal(3, document.ContentKeyPeriods.Count);

			Assert.Null(document.ContentKeyPeriods.ElementAt(0).Id);
			Assert.Equal(expectedPeriod1Index, document.ContentKeyPeriods.ElementAt(0).Index);
			Assert.Null(document.ContentKeyPeriods.ElementAt(0).Start);
			Assert.Null(document.ContentKeyPeriods.ElementAt(0).End);

			Assert.Equal(expectedPeriod2Id, document.ContentKeyPeriods.ElementAt(1).Id);
			Assert.Equal(expectedPeriod2Index, document.ContentKeyPeriods.ElementAt(1).Index);
			Assert.Null(document.ContentKeyPeriods.ElementAt(1).Start);
			Assert.Null(document.ContentKeyPeriods.ElementAt(1).End);

			Assert.Equal(expectedPeriod3Id, document.ContentKeyPeriods.ElementAt(2).Id);
			Assert.Null(document.ContentKeyPeriods.ElementAt(2).Index);
			Assert.Equal(expectedPeriod3Start, document.ContentKeyPeriods.ElementAt(2).Start);
			Assert.Equal(expectedPeriod3End, document.ContentKeyPeriods.ElementAt(2).End);
		}
	}
}