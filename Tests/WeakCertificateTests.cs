using System.IO;
using Xunit;

namespace Axinom.Cpix.Tests
{
	/// <summary>
	/// These tests verify that we do not accept weak certificates.
	/// </summary>
	public sealed class WeakCertificateTests
	{
		[Fact]
		public void SignDocument_WithWeakCertificate_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<WeakCertificateException>(() => document.SignedBy = TestHelpers.WeakSha1CertificateWithPrivateKey);
			Assert.Throws<WeakCertificateException>(() => document.SignedBy = TestHelpers.WeakSmallKeyCertificateWithPrivateKey);
		}

		[Fact]
		public void SignCollection_WithWeakCertificate_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<WeakCertificateException>(() => document.Recipients.AddSignature(TestHelpers.WeakSha1CertificateWithPrivateKey));
			Assert.Throws<WeakCertificateException>(() => document.Recipients.AddSignature(TestHelpers.WeakSmallKeyCertificateWithPrivateKey));
		}

		[Fact]
		public void AddRecipient_WithWeakCertificate_Fails()
		{
			var document = new CpixDocument();

			Assert.Throws<WeakCertificateException>(() => document.Recipients.Add(new Recipient(TestHelpers.WeakSha1CertificateWithPublicKey)));
			Assert.Throws<WeakCertificateException>(() => document.Recipients.Add(new Recipient(TestHelpers.WeakSmallKeyCertificateWithPublicKey)));
		}

		[Fact]
		public void LoadDocument_WithWeakCertificate_Fails()
		{
			Assert.Throws<WeakCertificateException>(() => CpixDocument.Load(new MemoryStream(), TestHelpers.WeakSha1CertificateWithPrivateKey));
			Assert.Throws<WeakCertificateException>(() => CpixDocument.Load(new MemoryStream(), TestHelpers.WeakSmallKeyCertificateWithPrivateKey));

		}
	}
}
