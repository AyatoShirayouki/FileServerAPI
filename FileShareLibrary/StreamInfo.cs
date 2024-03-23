using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileShareLibrary
{
	public class StreamInfo
	{
		public long Length { get; set; }
		public Stream Stream { get; set; }
		public ulong UnsignedLength => this.Length < 0 ? 0 : (ulong)this.Length;
	}
}
