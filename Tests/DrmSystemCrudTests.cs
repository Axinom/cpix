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
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid()
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("text is valid XML fragment"))
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<root1>XML fragment</root1><root2>can have multiple roots</root2>"))
			})));
			Assert.Null(Record.Exception(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<declaredprefix:test xmlns:declaredprefix=\"urn:test\"></declaredprefix:test>"))
			})));
		}

		[Fact]
		public void AddDrmSystem_WithIdenticalIdAndKeyIdTwice_Fails()
		{
			var document = new CpixDocument();

			document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid()
			});

			Assert.Throws<InvalidOperationException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = document.DrmSystems.First().Id,
				KeyId = document.DrmSystems.First().KeyId
			}));
		}

		[Fact]
		public void AddDrmSystem_WithVariousInvalidData_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.Empty,
				KeyId = Guid.NewGuid()
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.Empty
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = "<test>not base64-encoded XML fragment</test>"
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<bad></xmlfragment>"))
			}));
			Assert.Throws<InvalidCpixDataException>(() => document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<undeclaredprefix:test></undeclaredprefix:test>"))
			}));
		}

		[Fact]
		public void Save_WithValidDrmSystem_Succeeds()
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(TestHelpers.GenerateContentKey());

			document.DrmSystems.Add(new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = document.ContentKeys.First().Id,
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<test></test>"))
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
				Id = Guid.NewGuid(),
				KeyId = Guid.NewGuid(),
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<test></test>"))
			});

			Assert.Throws<InvalidCpixDataException>(() => document.Save(new MemoryStream()));
		}

		[Fact]
		public void Load_WithDrmSystemThatReferencesNonExistentContentKey_Fails()
		{
			const string CpixWithDrmSystemReferencingNonExistentContentKey = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz48Q1BJWCB4bWxuczp4c2k9Imh0dHA6Ly93d3cudzMub3JnLzIwMDEvWE1MU2NoZW1hLWluc3RhbmNlIiB4bWxuczp4c2Q9Imh0dHA6Ly93d3cudzMub3JnLzIwMDEvWE1MU2NoZW1hIiB4bWxucz0idXJuOmRhc2hpZjpvcmc6Y3BpeCIgeG1sbnM6ZHM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvMDkveG1sZHNpZyMiIHhtbG5zOmVuYz0iaHR0cDovL3d3dy53My5vcmcvMjAwMS8wNC94bWxlbmMjIiB4bWxuczpwc2tjPSJ1cm46aWV0ZjpwYXJhbXM6eG1sOm5zOmtleXByb3Y6cHNrYyI+PENvbnRlbnRLZXlMaXN0PjxDb250ZW50S2V5IGtpZD0iZTVlMjE1YmMtYmMwZS00NzZkLTg0MmYtZmExMjQyMDQzMDIwIj48RGF0YT48cHNrYzpTZWNyZXQ+PHBza2M6UGxhaW5WYWx1ZT5meitmZnpLTldwbm84Ymt3UWM1V0FnPT08L3Bza2M6UGxhaW5WYWx1ZT48L3Bza2M6U2VjcmV0PjwvRGF0YT48L0NvbnRlbnRLZXk+PC9Db250ZW50S2V5TGlzdD48RFJNU3lzdGVtTGlzdD48RFJNU3lzdGVtIHN5c3RlbUlkPSJhYzRmMDc3Ny1jOTU0LTRjMjEtYjdiNC0xOWM2MTMxYTQyOGYiIGtpZD0iNTY5YjdlNTUtMDMxNy00NTg5LTg4YWEtYmI3OGRiODA2Zjg4Ij48Q29udGVudFByb3RlY3Rpb25EYXRhPlBIUmxjM1ErUEM5MFpYTjBQZz09PC9Db250ZW50UHJvdGVjdGlvbkRhdGE+PC9EUk1TeXN0ZW0+PC9EUk1TeXN0ZW1MaXN0PjwvQ1BJWD4=";

			var ex = Assert.Throws<InvalidCpixDataException>(() =>
				CpixDocument.Load(new MemoryStream(Convert.FromBase64String(CpixWithDrmSystemReferencingNonExistentContentKey))));

			Assert.Contains("keys referenced by DRM systems", ex.Message);
		}

		[Fact]
		public void RoundTrip_WithSignedDrmSystemCollection_Succeeds()
		{
			var document = new CpixDocument();

			var contentKey = TestHelpers.GenerateContentKey();

			var drmSystem = new DrmSystem
			{
				Id = Guid.NewGuid(),
				KeyId = contentKey.Id,
				ContentProtectionData = Convert.ToBase64String(Encoding.UTF8.GetBytes("<test></test>"))
			};

			document.ContentKeys.Add(contentKey);
			document.DrmSystems.Add(drmSystem);
			document.DrmSystems.AddSignature(TestHelpers.Certificate1WithPrivateKey);
			document = TestHelpers.Reload(document);

			Assert.Single(document.ContentKeys);
			Assert.Single(document.DrmSystems);
			Assert.Equal(drmSystem.Id, document.DrmSystems.First().Id);
			Assert.Equal(drmSystem.KeyId, document.DrmSystems.First().KeyId);
			Assert.Equal(drmSystem.ContentProtectionData, document.DrmSystems.First().ContentProtectionData);
		}
	}
}