using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Text;

using Mono.CSharp;

namespace MonoDevelop.CSharpRepl
{
	public enum CSharpReplEvaluationResultType { NEED_MORE_INPUT, FAILED, SUCCESS_NO_OUTPUT, SUCCESS_WITH_OUTPUT }

	public class CSharpReplEvaluationResult
	{
		public CSharpReplEvaluationResultType ResultType { get; private set; }
		public string Result { get; private set; }

		public CSharpReplEvaluationResult(CSharpReplEvaluationResultType result_type, string result)
		{
			this.ResultType = result_type;
			this.Result = result;
		}

		public byte[] Serialize() 
		{
			byte[] type = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)this.ResultType));
			byte[] result = Encoding.UTF8.GetBytes(this.Result ?? "");
			byte[] full = new byte[type.Length + result.Length];
			type.CopyTo(full,0);
			result.CopyTo(full, type.Length);
			return full;			
		}
		
		public static CSharpReplEvaluationResult Deserialize(byte[] input)
		{
			CSharpReplEvaluationResultType type = (CSharpReplEvaluationResultType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(input,0));
			string result = input.Length - 4 > 0 ? Encoding.UTF8.GetString(input, 4, input.Length - 4) : null;
			return new CSharpReplEvaluationResult(type, result);
		}
	}
}
