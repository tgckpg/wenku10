using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.MigrationOps
{
	/// <summary>
	// Naive obfuscation trying to prevents *normal* user fiddling with critical files
	/// </summary>
	class NaiveObfustream : Stream
	{
		Stream Source;

		private byte IV;

		public NaiveObfustream( Stream SourceStream, byte IV )
		{
			this.IV = IV;
			Source = SourceStream;
		}

		public override bool CanRead => Source.CanRead;
		public override bool CanSeek => Source.CanSeek;
		public override bool CanWrite => Source.CanWrite;
		public override long Length => Source.Length;
		public override long Position { get => Source.Position; set => Source.Position = value; }
		public override void Flush() => Source.Flush();
		public override long Seek( long offset, SeekOrigin origin ) => Source.Seek( offset, origin );
		public override void SetLength( long value ) => Source.SetLength( value);

		public override int Read( byte[] buffer, int offset, int count )
		{
			int ret = Source.Read( buffer, offset, count );
			unchecked
			{
				for ( int i = offset, l = offset + ret; i < l; i++ )
				{
					buffer[ i ] -= IV;
				}
			}
			return ret;
		}

		public override void Write( byte[] buffer, int offset, int count )
		{
			unchecked
			{
				for ( int i = offset, l = offset + count; i < l; i++ )
				{
					Source.WriteByte( ( byte ) ( buffer[ i ] + IV ) );
				}
			}
		}
	}
}