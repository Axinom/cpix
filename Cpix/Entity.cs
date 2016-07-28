using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axinom.Cpix
{
	/// <summary>
	/// Abstract base class for all types of CPIX entities that can be managed via CpixDocument.
	/// </summary>
	public abstract class Entity
	{
		/// <summary>
		/// Only this library can define implementations!
		/// </summary>
		internal Entity()
		{

		}
	}
}
