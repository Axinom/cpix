using Axinom.Cpix.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Axinom.Cpix.TestVectorGenerator
{
	sealed class EncryptedContentKeys : ITestVector
	{
		private static readonly Dictionary<Guid, string> _contentKeys = new Dictionary<Guid, string>
		{
			{ new Guid("bd5adf51-cf04-410f-aac3-ec63a69e929e"), "3rWoHYasQubO6HbJGrGtLw==" },
			{ new Guid("d2920429-87ab-41e6-a4c5-a8c836b6312e"), "O5w9FdZiwmQK4uIXzAziaQ==" },
			{ new Guid("e17ba4b8-faff-4d30-bcba-7485e3f2e884"), "Cwu/3hSBBRQ7SurBdZD5ow==" },
			{ new Guid("0ae6b9ad-92d2-4ebe-882b-1d07dee70715"), "FB6/Eck9Y9SXy6bY8UU/Mw==" },
		};

		public string Description
		{
			get
			{
				var sb = new StringBuilder();
				sb.AppendLine("Content keys encrypted for delivery to a specific recipient (Cert1).");
				sb.AppendLine();
				sb.AppendLine("The decrypted values of the content keys (base64-encoded here) are: ");
				sb.AppendLine();

				foreach (var pair in _contentKeys)
					sb.AppendLine($"* {pair.Value} with ID {pair.Key}.");

				return sb.ToString();
			}
		}

		public bool OutputIsValid => true;

		public void Generate(Stream outputStream)
		{
			var document = new CpixDocument();

			foreach (var pair in _contentKeys)
				document.ContentKeys.Add(new ContentKey
				{
					Id = pair.Key,
					Value = Convert.FromBase64String(pair.Value)
				});

			document.Recipients.Add(new Recipient(TestHelpers.Certificate1WithPublicKey));

			document.Save(outputStream);
		}
	}
}
