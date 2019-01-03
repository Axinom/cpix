using Axinom.Cpix.Tests;
using System;
using System.IO;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class ClearContentKeysOnly : ITestVector
	{
		public string Description => "Content keys with values in the clear (without encryption).";
		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("40d02dd1-61a3-4787-a155-572325d47b80"),
				Value = Convert.FromBase64String("gPxt0PMwrHM4TdjwdQmhhQ==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("0a30ea4f-539d-4b02-94b2-2b3fba2576d3"),
				Value = Convert.FromBase64String("x/gaoS/fDi8BqGNIhkixwQ==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("9f7908fa-5d5c-4097-ba53-50edc2235fbc"),
				Value = Convert.FromBase64String("3iv9lYwafpe0uEmxDc6PSw==")
			});
			document.ContentKeys.Add(new ContentKey
			{
				Id = new Guid("fac2cbf5-889c-412b-a385-04a29d409bdc"),
				Value = Convert.FromBase64String("1OZVZZoYFSU2X/7qT3sHwg==")
			});

			document.Save(outputStream);
		}
	}
}
