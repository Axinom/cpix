namespace Axinom.Cpix
{
	/// <summary>
	/// Represents the DRM system signaling data to be inserted into HLS playlists.
	/// </summary>
	public class HlsSignalingData
	{
		/// <summary>
		/// Gets or sets the signaling data to be inserted into the HLS master
		/// playlist. The data includes the EXT-X-SESSION-KEY tag along with any
		/// proprietary tags. This is UTF-8 text without a byte order mark that may
		/// contain multiple lines.
		/// </summary>
		public string MasterPlaylistData;

		/// <summary>
		/// Gets or sets the signaling data to be inserted into the HLS variant
		/// playlist. The data includes the EXT-X-KEY tag along with any proprietary
		/// tags. This is UTF-8 text without a byte order mark that may contain
		/// multiple lines.
		/// </summary>
		public string VariantPlaylistData;
	}
}