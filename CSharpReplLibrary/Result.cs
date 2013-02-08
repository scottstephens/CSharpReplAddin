using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Text;

using Mono.CSharp;

namespace MonoDevelop.CSharpRepl
{
	public enum ResultType { NEED_MORE_INPUT, FAILED, SUCCESS_NO_OUTPUT, SUCCESS_WITH_OUTPUT }

	public class Result
	{
		public ResultType Type { get; private set; }
		public string ResultMessage { get; private set; }

		public Result(ResultType result_type, string result)
		{
			this.Type = result_type;
			this.ResultMessage = result;
		}

		public byte[] Serialize() 
		{
			byte[] type = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)this.Type));
			byte[] result = Encoding.UTF8.GetBytes(this.ResultMessage ?? "");
			byte[] full = new byte[type.Length + result.Length];
			type.CopyTo(full,0);
			result.CopyTo(full, type.Length);
			return full;			
		}
		
		public static Result Deserialize(byte[] input)
		{
			ResultType type = (ResultType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(input,0));
			string result = input.Length - 4 > 0 ? Encoding.UTF8.GetString(input, 4, input.Length - 4) : null;
			return new Result(type, result);
		}
	}
}
