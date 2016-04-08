using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axinom.Cpix
{
	public sealed class ContentKey
	{
		public Guid Id { get; set; }

		/// <summary>
		/// The clear (nonencrypted) value of the content key.
		/// </summary>
		public byte[] Value { get; set; }
	}
}
