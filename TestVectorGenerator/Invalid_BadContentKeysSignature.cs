using Axinom.Cpix.Tests;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class Invalid_BadContentKeysSignature : ITestVector
	{
		public string Description => @"The signature on the content key collection should fail validation because one of the content key elements was removed after applying the signature.";
		public bool OutputIsValid => false;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("c003b21a-fe68-4162-a809-b0add9fe49c1"),
				Value = Convert.FromBase64String("fV2AfUA1WnvFaySrl6I7vg==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("a3813bc3-986a-462b-84d1-c5d17d3ac0f5"),
				Value = Convert.FromBase64String("fs1XSl6sqULnEjX0g6UyBg==")
			});

			document.ContentKeys.AddSignature(TestHelpers.Certificate4WithPrivateKey);

			var buffer = new MemoryStream();
			document.Save(buffer);

			var xml = new XmlDocument();
			buffer.Position = 0;
			xml.Load(buffer);

			var namespaces = XmlHelpers.CreateCpixNamespaceManager(xml);

			var firstContentKey = (XmlElement)xml.SelectSingleNode("/cpix:CPIX/cpix:ContentKeyList/cpix:ContentKey", namespaces);
			firstContentKey.ParentNode.RemoveChild(firstContentKey);

			using (var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				CloseOutput = false
			}))
			{
				xml.Save(writer);
			}
		}
	}
}
