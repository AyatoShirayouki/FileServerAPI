namespace FileShareLibrary
{
	public class StreamInfo
	{
		public long Length { get; set; }
		public Stream Stream { get; set; }
		public ulong UnsignedLength => this.Length < 0 ? 0 : (ulong)this.Length;
	}
}
