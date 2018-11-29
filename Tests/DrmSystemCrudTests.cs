using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Axinom.Cpix.Tests
{
	public sealed class DrmSystemCrudTests
	{
		[Fact]
		public void AddDrmSystem_WithVariousValidData_Succeeds()
		{
			var document = new CpixDocument();

			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid()
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "text is valid XML fragment"
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "<root1>XML fragment</root1><root2>can have multiple roots</root2>"
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "<declaredprefix:test xmlns:declaredprefix=\"urn:test\"></declaredprefix:test>"
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				Pssh = Convert.ToBase64String(new byte[] { 0x11, 0x22, 0xFF })
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HlsSignalingData = new HlsSignalingData()
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "test",
					VariantPlaylistData = "test"
				}
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				SmoothStreamingProtectionHeaderData = "test"
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HdsSignalingData = "<aa:drmAdditionalHeader xmlns:aa=\"urn:test\">data</aa:drmAdditionalHeader>"
			})));
		}

		[Fact]
		public void AddDrmSystem_WithVariousInvalidData_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.Empty,
				KeyId = Guid.NewGuid()
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.Empty
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "<bad></xmlfragment>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "<undeclaredprefix:test></undeclaredprefix:test>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HdsSignalingData = "<drmAdditionalHeader></drmAdditionalHeader><drmAdditionalHeader></drmAdditionalHeader>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HdsSignalingData = "<unexpectedHdsHeader></unexpectedHdsHeader>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				HdsSignalingData = "<undeclaredprefix:drmAdditionalHeader></undeclaredprefix:drmAdditionalHeader>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				Pssh = "notbase64!"
			}));
		}

		[Fact]
		public void AddDrmSystem_WithIdenticalSystemIdAndKeyIdTwice_Fails()
		{
			var document = new CpixDocument();

			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid()
			});

			Assert.Throws<InvalidOperationException>(() => document.DrmSystems.Add(new DrmSystem
			{
				SystemId = document.DrmSystems.First().SystemId,
				KeyId = document.DrmSystems.First().KeyId
			}));
		}

		[Fact]
		public void Save_WithValidDrmSystem_Succeeds()
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = document.ContentKeys.First().Id,
				ContentProtectionData = "<pssh>data</pssh>",
				HdsSignalingData = "<drmAdditionalHeader>data</drmAdditionalHeader>",
				Pssh = Convert.ToBase64String(new byte[] { 0x11, 0x22, 0x33 }),
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "hlsmasterdata1",
					VariantPlaylistData = "hlsvariantdata2"
				},
				SmoothStreamingProtectionHeaderData = "smoothstreamingdata"
			});

			Assert.Null(Record.Exception(() => document.Save(new MemoryStream())));
		}

		[Fact]
		public void Save_WithDrmSystemThatReferencesNonExistentContentKey_Fails()
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.DrmSystems.Add(new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = Guid.NewGuid()
			});

			Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void Load_WithCpixContainingDrmSystemElementThatReferencesNonExistentContentKey_Fails()
		{
			const string CpixWithDrmSystemReferencingNonExistentContentKey = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48Q1BJWCB4bWxuczp4c2k9Imh0dHA6Ly93d3cudzMub3JnLzIwMDEvWE1MU2NoZW1hLWluc3RhbmNlIiB4bWxuczp4c2Q9Imh0dHA6Ly93d3cudzMub3JnLzIwMDEvWE1MU2NoZW1hIiB4bWxucz0idXJuOmRhc2hpZjpvcmc6Y3BpeCIgeG1sbnM6ZHM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvMDkveG1sZHNpZyMiIHhtbG5zOmVuYz0iaHR0cDovL3d3dy53My5vcmcvMjAwMS8wNC94bWxlbmMjIiB4bWxuczpwc2tjPSJ1cm46aWV0ZjpwYXJhbXM6eG1sOm5zOmtleXByb3Y6cHNrYyI+PENvbnRlbnRLZXlMaXN0PjxDb250ZW50S2V5IGtpZD0iZTVlMjE1YmMtYmMwZS00NzZkLTg0MmYtZmExMjQyMDQzMDIwIj48RGF0YT48cHNrYzpTZWNyZXQ+PHBza2M6UGxhaW5WYWx1ZT5meitmZnpLTldwbm84Ymt3UWM1V0FnPT08L3Bza2M6UGxhaW5WYWx1ZT48L3Bza2M6U2VjcmV0PjwvRGF0YT48L0NvbnRlbnRLZXk+PC9Db250ZW50S2V5TGlzdD48RFJNU3lzdGVtTGlzdD48RFJNU3lzdGVtIHN5c3RlbUlkPSJhYzRmMDc3Ny1jOTU0LTRjMjEtYjdiNC0xOWM2MTMxYTQyOGYiIGtpZD0iNTY5YjdlNTUtMDMxNy00NTg5LTg4YWEtYmI3OGRiODA2Zjg4Ij48Q29udGVudFByb3RlY3Rpb25EYXRhPlBIUmxjM1ErUEM5MFpYTjBQZz09PC9Db250ZW50UHJvdGVjdGlvbkRhdGE+PC9EUk1TeXN0ZW0+PC9EUk1TeXN0ZW1MaXN0PjwvQ1BJWD4=";

			var ex = Assert.Throws<InvalidCpixDataException>(() =>
				CpixDocument.Load(new MemoryStream(Convert.FromBase64String(CpixWithDrmSystemReferencingNonExistentContentKey))));

			Assert.Contains("keys referenced by DRM system", ex.Message);
		}

		[Fact]
		public void Load_WithCpixContainingMultipleDrmSystemsWithIdenticalSystemIdAndKeyId_Fails()
		{
			const string CpixWithInvalidDrmSystems = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyList><ContentKey kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"><Data><pskc:Secret><pskc:PlainValue>AQIDBAUGBwgJCgECAwQFBg==</pskc:PlainValue></pskc:Secret></Data></ContentKey></ContentKeyList><DRMSystemList><DRMSystem systemId=\"edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\" kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"></DRMSystem><DRMSystem systemId=\"edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\" kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"></DRMSystem></DRMSystemList></CPIX>";

			var ex = Assert.Throws<InvalidCpixDataException>(() =>
				CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(CpixWithInvalidDrmSystems))));

			Assert.Contains("multiple DRM system signaling entries", ex.Message);
		}

		[Fact]
		public void Load_WithCpixContainingSingleHlsSignalingDataElementWithoutPlaylistAttribute_SucceedsWithDataInterpretedAsVariantPlaylistData()
		{
			const string CpixWithHlsSignalingDataWithoutPlaylistAttribute = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyList><ContentKey kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"><Data><pskc:Secret><pskc:PlainValue>AQIDBAUGBwgJCgECAwQFBg==</pskc:PlainValue></pskc:Secret></Data></ContentKey></ContentKeyList><DRMSystemList><DRMSystem systemId=\"edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\" kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"><HLSSignalingData>YWE=</HLSSignalingData></DRMSystem></DRMSystemList></CPIX>";

			var document = CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(CpixWithHlsSignalingDataWithoutPlaylistAttribute)));
			var drmSystem = document.DrmSystems.First();

			Assert.NotNull(drmSystem.HlsSignalingData.VariantPlaylistData);
			Assert.Null(drmSystem.HlsSignalingData.MasterPlaylistData);
		}

		[Fact]
		public void Load_WithCpixContainingManyHlsSignalingDataElementsAndOneWithoutPlaylistAttribute_Fails()
		{
			const string CpixWithOneSignalingDataElementWithoutPlaylistAttribute = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CPIX xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"urn:dashif:org:cpix\" xmlns:ds=\"http://www.w3.org/2000/09/xmldsig#\" xmlns:enc=\"http://www.w3.org/2001/04/xmlenc#\" xmlns:pskc=\"urn:ietf:params:xml:ns:keyprov:pskc\"><ContentKeyList><ContentKey kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"><Data><pskc:Secret><pskc:PlainValue>AQIDBAUGBwgJCgECAwQFBg==</pskc:PlainValue></pskc:Secret></Data></ContentKey></ContentKeyList><DRMSystemList><DRMSystem systemId=\"edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\" kid=\"f8c80c25-690f-4736-8132-430e5c6994ce\"><HLSSignalingData playlist=\"master\">YWE=</HLSSignalingData><HLSSignalingData>YWE=</HLSSignalingData></DRMSystem></DRMSystemList></CPIX>";

			var ex = Assert.Throws<InvalidCpixDataException>(() =>
				CpixDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(CpixWithOneSignalingDataElementWithoutPlaylistAttribute))));

			Assert.Contains("only one HLSSignalingData element", ex.Message);
		}

		[Fact]
		public void RoundTrip_WithSignedDrmSystemCollection_Succeeds()
		{
			var document = new CpixDocument();

			var contentKey = TestHelpers.GenerateContentKey();

			var drmSystem = new DrmSystem
			{
				SystemId = Guid.NewGuid(),
				KeyId = contentKey.Id,
				ContentProtectionData = "<pssh>data</pssh>",
				HdsSignalingData = "<drmAdditionalHeader>data</drmAdditionalHeader>",
				Pssh = Convert.ToBase64String(new byte[] { 0x11, 0x22, 0x33 }),
				HlsSignalingData = new HlsSignalingData
				{
					MasterPlaylistData = "hlsmasterdata1",
					VariantPlaylistData = "hlsvariantdata2"
				},
				SmoothStreamingProtectionHeaderData = "smoothstreamingdata"
			};

			document.ContentKeys.Add(contentKey);
			document.DrmSystems.Add(drmSystem);
			document.DrmSystems.AddSignature(TestHelpers.Certificate1WithPrivateKey);
			document = TestHelpers.Reload(document);

			Assert.Single(document.ContentKeys);
			Assert.Single(document.DrmSystems);
			Assert.Equal(drmSystem.SystemId, document.DrmSystems.First().SystemId);
			Assert.Equal(drmSystem.KeyId, document.DrmSystems.First().KeyId);
			Assert.Equal(drmSystem.ContentProtectionData, document.DrmSystems.First().ContentProtectionData);
			Assert.Equal(drmSystem.HdsSignalingData, document.DrmSystems.First().HdsSignalingData);
			Assert.Equal(drmSystem.Pssh, document.DrmSystems.First().Pssh);
			Assert.Equal(drmSystem.HlsSignalingData.MasterPlaylistData, document.DrmSystems.First().HlsSignalingData.MasterPlaylistData);
			Assert.Equal(drmSystem.HlsSignalingData.VariantPlaylistData, document.DrmSystems.First().HlsSignalingData.VariantPlaylistData);
			Assert.Equal(drmSystem.SmoothStreamingProtectionHeaderData, document.DrmSystems.First().SmoothStreamingProtectionHeaderData);
		}
	}
}