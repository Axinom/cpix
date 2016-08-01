using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axinom.Cpix
{
	public interface IContentKey
	{
		/// <summary>
		/// Unique ID of the content key.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the value of the content key.
		/// 
		/// This is null if the values are encrypted and a decryption key is not available.
		/// </summary>
		byte[] Value { get; }
	}
}
