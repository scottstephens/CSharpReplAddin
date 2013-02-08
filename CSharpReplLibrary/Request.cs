using System;
using System.Text;
using System.Net;

namespace MonoDevelop.CSharpRepl
{
	public enum RequestType { Evaluate, LoadAssembly, Variables, Usings }

	public class Request
	{
		public RequestType Type { get; set; }
		public string Code { get; set; }
		public string AssemblyToLoad { get; set; }

		protected Request(RequestType type, string code=null, string assembly=null)
		{
			this.Type = type;
			this.Code = code;
			this.AssemblyToLoad = assembly;
		}

		public static Request CreateEvaluationRequest(string code)
		{
			return new Request(RequestType.Evaluate, code, null);
		}
		public static Request CreateAssemblyLoadRequest(string assembly)
		{
			return new Request(RequestType.Evaluate, null, assembly);
		}

		public static Request CreateGetVariablesRequest()
		{
			return new Request(RequestType.Variables, null, null);
		}

		public static Request CreateGetUsingsRequest()
		{
			return new Request(RequestType.Usings, null, null);
		}

		public byte[] Serialize() 
		{
			byte[] type = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)this.Type));
			if (this.Type == RequestType.Evaluate) {
				byte[] result = Encoding.UTF8.GetBytes(this.Code ?? "");
				byte[] full = new byte[type.Length + result.Length];
				type.CopyTo(full,0);
				result.CopyTo(full, type.Length);
				return full;
			} else if (this.Type == RequestType.LoadAssembly) {
				byte[] result = Encoding.UTF8.GetBytes(this.AssemblyToLoad ?? "");
				byte[] full = new byte[type.Length + result.Length];
				type.CopyTo(full,0);
				result.CopyTo(full, type.Length);
				return full;	
			} else {
				return type;
			}
		}

		public static Request Deserialize(byte[] input)
		{
			RequestType type = (RequestType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(input,0));
			if (type == RequestType.Evaluate) {
				string result = input.Length - 4 > 0 ? Encoding.UTF8.GetString(input, 4, input.Length - 4) : null;
				return CreateEvaluationRequest(result);
			} else if (type == RequestType.LoadAssembly) {
				string result = input.Length - 4 > 0 ? Encoding.UTF8.GetString(input, 4, input.Length - 4) : null;
				return CreateAssemblyLoadRequest(result);
			} else {
				return new Request(type);
			}
		}
	}
}

