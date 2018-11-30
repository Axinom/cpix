using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Axinom.Cpix
{
	public enum ByteOrder
	{
		BigEndian,
		LittleEndian
	}

	/// <summary>
	/// Binary writer with variable byte endianness.
	/// Note that text writing does not support big-endian byte order. This is for binary only.
	/// </summary>
	public class MultiEndianBinaryWriter : BinaryWriter
	{
		/// <summary>
		/// Gets or sets the byte order the writer uses for its operations.
		/// </summary>
		public ByteOrder ByteOrder { get; set; }

		public override void Write(double value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(short value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(ushort value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(int value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(uint value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(long value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(ulong value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		public override void Write(float value)
		{
			if (ByteOrder == ByteOrder.LittleEndian)
			{
				base.Write(value);
				return;
			}

			Write(BitConverter.GetBytes(value).Reverse().ToArray());
		}

		#region Initialization
		public MultiEndianBinaryWriter(Stream output, ByteOrder byteOrder)
			: base(output)
		{
			if (!BitConverter.IsLittleEndian)
				throw new InvalidOperationException("This class is only designed for little endian machines and will almost certainly not function correctly on big endian machines.");

			ByteOrder = byteOrder;
		}

		public MultiEndianBinaryWriter(Stream output, Encoding encoding, ByteOrder byteOrder)
			: base(output, encoding)
		{
			if (!BitConverter.IsLittleEndian)
				throw new InvalidOperationException("This class is only designed for little endian machines and will almost certainly not function correctly on big endian machines.");

			ByteOrder = byteOrder;
		}
		#endregion
	}
}