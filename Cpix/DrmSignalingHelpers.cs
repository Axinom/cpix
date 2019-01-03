using ProtoBuf;
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using shaka.media;

namespace Axinom.Cpix
{
	/// <summary>
	/// Helpers for generating DRM signaling data in common scenarios. These are primarily meant for generating
	/// realistic example data but may also be suitable for production scenarios. The flexibility can be rather limited,
	/// though.
	/// 
	/// You can use the decoders at https://tools.axinom.com/ to verify the generated structures are valid.
	/// 
	/// No guarantees are made about API compatibility here - this is left public mostly because it is annoying
	/// code to write and if we can help someone out by having them use this tested implementation, so be it.
	/// </summary>
	public static class DrmSignalingHelpers
	{
		public static readonly Guid FairPlaySystemId = new Guid("94ce86fb-07ff-4f43-adB8-93d2fa968ca2");
		public static readonly Guid PlayReadySystemId = new Guid("9a04f079-9840-4286-ab92-e65be0885f95");
		public static readonly Guid WidevineSystemId = new Guid("edef8ba9-79d6-4ace-a3c8-27dcd51d21ed");

		/// <summary>
		/// Adds default DRM system signaling entries for all keys.
		/// </summary>
		public static void AddDefaultSignalingForAllKeys(CpixDocument document)
		{
			foreach (var key in document.ContentKeys)
			{
				document.DrmSystems.Add(new DrmSystem
				{
					SystemId = WidevineSystemId,
					KeyId = key.Id,
					ContentProtectionData = GenerateWidevineDashSignaling(key.Id),
					HlsSignalingData = new HlsSignalingData
					{
						MasterPlaylistData = GenerateWidevineHlsMasterPlaylistSignaling(key.Id),
						MediaPlaylistData = GenerateWidevineHlsMediaPlaylistSignaling(key.Id),
					}
				});
				document.DrmSystems.Add(new DrmSystem
				{
					SystemId = PlayReadySystemId,
					KeyId = key.Id,
					ContentProtectionData = GeneratePlayReadyDashSignaling(key.Id),
					SmoothStreamingProtectionHeaderData = GeneratePlayReadyMssSignaling(key.Id)
				});
				document.DrmSystems.Add(new DrmSystem
				{
					SystemId = FairPlaySystemId,
					KeyId = key.Id,
					HlsSignalingData = new HlsSignalingData
					{
						MasterPlaylistData = GenerateFairPlayHlsMasterPlaylistSignaling(key.Id),
						MediaPlaylistData = GenerateFairPlayHlsMediaPlaylistSignaling(key.Id)
					}
				});
			}
		}

		public static string GeneratePlayReadyDashSignaling(Guid keyId)
		{
			var psshBoxContents = GeneratePlayReadyHeader(keyId);
			var psshBox = CreatePsshBox(PlayReadySystemId, psshBoxContents);

			var psshElement = new XElement(DashConstants.PsshName, Convert.ToBase64String(psshBox));
			var proElement = new XElement(DashConstants.ProName, Convert.ToBase64String(psshBoxContents));

			return psshElement.ToString() + proElement.ToString();
		}

		public static string GenerateWidevineDashSignaling(Guid keyId)
		{
			var psshBoxContents = GenerateWidevineHeader(keyId);
			var psshBox = CreatePsshBox(WidevineSystemId, psshBoxContents);
			var psshElement = new XElement(DashConstants.PsshName, Convert.ToBase64String(psshBox));

			return psshElement.ToString();
		}

		public static string GeneratePlayReadyMssSignaling(Guid keyId)
		{
			// For MSS the signaling is the base64-encoded PRO.

			var psshBoxContents = GeneratePlayReadyHeader(keyId);
			return Convert.ToBase64String(psshBoxContents);
		}

		public static string GenerateWidevineHlsMasterPlaylistSignaling(Guid keyId)
		{
			return $"#EXT-X-SESSION-KEY:{GenerateWidevineHlsAttributes(keyId)}";
		}

		public static string GenerateWidevineHlsMediaPlaylistSignaling(Guid keyId)
		{
			return $"#EXT-X-KEY:{GenerateWidevineHlsAttributes(keyId)}";
		}

		public static string GenerateFairPlayHlsMasterPlaylistSignaling(Guid keyId)
		{
			return $"#EXT-X-SESSION-KEY:{GenerateFairPlayHlsAttributes(keyId)}";
		}

		public static string GenerateFairPlayHlsMediaPlaylistSignaling(Guid keyId)
		{
			return $"#EXT-X-KEY:{GenerateFairPlayHlsAttributes(keyId)}";
		}

		public static byte[] GeneratePlayReadyPsshBox(Guid keyId)
		{
			var psshBoxContents = GeneratePlayReadyHeader(keyId);

			return CreatePsshBox(PlayReadySystemId, psshBoxContents);
		}

		public static byte[] GenerateWidevinePsshBox(Guid keyId)
		{
			var psshBoxContents = GenerateWidevineHeader(keyId);

			return CreatePsshBox(WidevineSystemId, psshBoxContents);
		}

		/// <summary>
		/// Creates a PSSH (Protection System Specific Header) box suitable for embedding into a media
		/// file that follows the ISO Base Media File Format specification.
		/// </summary>
		private static byte[] CreatePsshBox(Guid systemId, byte[] data)
		{
			// Size (32) BE
			// Type (32)
			// Version (8)
			// Flags (24)
			// SystemID (16*8) BE
			// DataSize (32) BE
			// Data (DataSize*8)

			using (var buffer = new MemoryStream())
			{
				using (var writer = new MultiEndianBinaryWriter(buffer, ByteOrder.BigEndian))
				{
					writer.Write(4 + 4 + 1 + 3 + 16 + 4 + data.Length);
					writer.Write(new[] { 'p', 's', 's', 'h' });
					writer.Write(0); // 0 flags, 0 version.
					writer.Write(systemId.ToBigEndianByteArray());
					writer.Write(data.Length);
					writer.Write(data);
				}

				return buffer.ToArray();
			}
		}

		private static class DashConstants
		{
			private const string MpdNamespace = "urn:mpeg:dash:schema:mpd:2011";
			private const string CencNamespace = "urn:mpeg:cenc:2013";
			private const string PlayReadyNamespace = "urn:microsoft:playready";

			public static readonly XName ContentProtectionName = XName.Get("ContentProtection", MpdNamespace);
			public static readonly XName PsshName = XName.Get("pssh", CencNamespace);
			public static readonly XName ProName = XName.Get("pro", PlayReadyNamespace);

			public static string GetProtectionSystemSchemeIdUri(Guid systemId)
			{
				return $"urn:uuid:{systemId}";
			}
		}

		private static byte[] GenerateWidevineHeader(Guid keyId)
		{
			using (var buffer = new MemoryStream())
			{
				var widevineHeader = new WidevinePsshData()
				{
					KeyIds = { keyId.ToBigEndianByteArray() },
					ProtectionScheme = 1667591779 // "cenc".
				};

				Serializer.Serialize(buffer, widevineHeader);

				return buffer.ToArray();
			}
		}

		private static byte[] GeneratePlayReadyHeader(Guid keyId)
		{
			var kidString = Convert.ToBase64String(keyId.ToByteArray());

			// Plain text manipulation here to keep things simple. Some common issues include:
			// 1) The first element must be EXACTLY as written here. Including small things like order of attributes.
			// 2) There must be no extra whitespace anywhere.
			var xml = $"<WRMHEADER xmlns=\"http://schemas.microsoft.com/DRM/2007/03/PlayReadyHeader\" version=\"4.0.0.0\"><DATA><PROTECTINFO><KEYLEN>16</KEYLEN><ALGID>AESCTR</ALGID></PROTECTINFO><KID>{kidString}</KID></DATA></WRMHEADER>";

			var xmlBytes = Encoding.Unicode.GetBytes(xml);

			using (var buffer = new MemoryStream())
			{
				using (var writer = new BinaryWriter(buffer))
				{
					// Size (32)
					// RecordCount (16)
					//		RecordType (16)
					//		RecordLength (16)
					//		Data (xml)

					writer.Write(xmlBytes.Length + 4 + 2 + 2 + 2); // Length.
					writer.Write((ushort)1); // Record count.
					writer.Write((ushort)1); // Record type (RM header).
					writer.Write((ushort)xmlBytes.Length); // Record length.
					writer.Write(xmlBytes);
				}

				return buffer.ToArray();
			}
		}

		private static string GenerateWidevineHlsAttributes(Guid keyId)
		{
			const string widevineMethodValue = "SAMPLE-AES-CTR";

			var psshBoxContents = GenerateWidevineHeader(keyId);
			var psshBox = CreatePsshBox(WidevineSystemId, psshBoxContents);
			var psshBoxAsBase64 = Convert.ToBase64String(psshBox);

			// Widevine uses KEYID as 0x1234 hex string. Big endian, naturally.
			var keyIdString = "0x" + ByteArrayToHexString(keyId.ToBigEndianByteArray());

			var widevineAttributes = $"METHOD={widevineMethodValue},URI=\"data:text/plain;base64,{psshBoxAsBase64}\",KEYID={keyIdString},KEYFORMAT=\"urn:uuid:edef8ba9-79d6-4ace-a3c8-27dcd51d21ed\",KEYFORMATVERSIONS=\"1\"";

			return widevineAttributes;
		}

		private static string GenerateFairPlayHlsAttributes(Guid keyId)
		{
			return $"METHOD=SAMPLE-AES,URI=\"skd://{keyId}\",KEYFORMAT=\"com.apple.streamingkeydelivery\",KEYFORMATVERSIONS=\"1\"";
		}

		private static string ByteArrayToHexString(byte[] bytes)
		{
			var hex = BitConverter.ToString(bytes);
			return hex.Replace("-", "");
		}

		/// <summary>
		/// Serializes the GUID to a byte array, using the big endian format for all components.
		/// This format is often used by non-Microsoft tooling.
		/// </summary>
		private static byte[] ToBigEndianByteArray(this Guid guid)
		{
			if (!BitConverter.IsLittleEndian)
				throw new InvalidOperationException("This method has not been tested on big endian machines and likely would not operate correctly.");

			var bytes = guid.ToByteArray();

			Array.Reverse(bytes, 0, 4);
			Array.Reverse(bytes, 4, 2);
			Array.Reverse(bytes, 6, 2);

			return bytes;
		}
	}
}