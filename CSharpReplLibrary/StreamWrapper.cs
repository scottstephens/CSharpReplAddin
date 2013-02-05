using System;
using System.IO;
using System.Net;

namespace MonoDevelop.CSharpRepl
{
	public class StreamedMessageUtils<T> where T : Stream 
	{
		public T UnderlyingStream { get; set; }

		public StreamedMessageUtils (T underlying_stream)
		{
			this.UnderlyingStream = underlying_stream;
		}

		public void writeMessage(byte[] message)
		{
			if (message.Length > Int32.MaxValue)
				throw new Exception(String.Format("Cannot send message whose length is greater than {0} bytes",Int32.MaxValue));

			byte[] length = BitConverter.GetBytes(message.Length);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(length);

			this.UnderlyingStream.Write(length, 0, 4);
			this.UnderlyingStream.Write(message, 0, message.Length);
			this.UnderlyingStream.Flush();
		}

		public byte[] readMessage()
		{
			byte[] raw_size = this.readFixedSize(4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(raw_size);
			int size = BitConverter.ToInt32(raw_size,0);
			byte[] message = this.readFixedSize(size);
			return message;
		}

		private byte[] readFixedSize(int size)
		{
			int read = 0;
			byte[] buffer = new byte[size];
			while (read < size)
			{
				read += this.UnderlyingStream.Read(buffer, read, size - read);
			}
			return buffer;
		}
	}
}

