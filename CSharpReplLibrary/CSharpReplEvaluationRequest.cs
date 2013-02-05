using System;
using System.Text;

namespace MonoDevelop.CSharpRepl
{
	public class CSharpReplEvaluationRequest
	{
		public string Code { get; set; }

		public CSharpReplEvaluationRequest(string code)
		{
			this.Code = code;
		}

		public byte[] Serialize() 
		{
			return Encoding.UTF8.GetBytes(this.Code);
		}

		public static CSharpReplEvaluationRequest Deserialize(byte[] input)
		{
			return new CSharpReplEvaluationRequest(Encoding.UTF8.GetString(input));
		}
	}
}

