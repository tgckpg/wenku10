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
		private long InitialPos;

		public NaiveObfustream( Stream SourceStream, byte IV )
		{
			this.IV = IV;
			Source = SourceStream;
			InitialPos = Source.Position;
		}

		public override bool CanRead => Source.CanRead;
		public override bool CanSeek => Source.CanSeek;
		public override bool CanWrite => Source.CanWrite;
		public override void Flush() => Source.Flush();

		public override long Length => Source.Length - InitialPos;

		public override long Position
		{
			get => Source.Position - InitialPos;
			set => Source.Position = value + InitialPos;
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			if( origin == SeekOrigin.Begin )
			{
				return Source.Seek( offset + InitialPos, origin );
			}
			return Source.Seek( offset, origin );
		}

		public override void SetLength( long value ) => Source.SetLength( value + InitialPos );

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