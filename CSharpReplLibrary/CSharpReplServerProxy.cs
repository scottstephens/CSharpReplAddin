using System;
using System.Net;
using System.Net.Sockets;

namespace MonoDevelop.CSharpRepl
{
	public class CSharpReplServerProxy : ICSharpRepl
	{
		public TcpClient Client { get; private set; }
		public IPEndPoint RemoteAddress { get; private set; }

		public CSharpReplServerProxy(int port)
		{
			this.Client = new TcpClient();
			this.RemoteAddress = new IPEndPoint(IPAddress.Loopback, port);
		}
		public void Start()
		{
			this.Client.Connect(this.RemoteAddress);
		}
		#region ICSharpShell implementation

		public Result evaluate (string input)
		{
			StreamedMessageUtils<NetworkStream> messenger = new StreamedMessageUtils<NetworkStream>(this.Client.GetStream());

			var request = Request.CreateEvaluationRequest(input);
			byte[] outgoing_buffer = request.Serialize();
			messenger.writeMessage(outgoing_buffer);

			byte[] incoming_buffer = messenger.readMessage();
			var result = Result.Deserialize(incoming_buffer);

			return result;
		}

		public Result loadAssembly(string file)
		{
			StreamedMessageUtils<NetworkStream> messenger = new StreamedMessageUtils<NetworkStream>(this.Client.GetStream());
			
			var request = Request.CreateLoadAssemblyRequest(file);
			byte[] outgoing_buffer = request.Serialize();
			messenger.writeMessage(outgoing_buffer);
			
			byte[] incoming_buffer = messenger.readMessage();
			var result = Result.Deserialize(incoming_buffer);
			
			return result;
		}

		public Result getVariables()
		{
			StreamedMessageUtils<NetworkStream> messenger = new StreamedMessageUtils<NetworkStream>(this.Client.GetStream());
			
			var request = Request.CreateGetVariablesRequest();
			byte[] outgoing_buffer = request.Serialize();
			messenger.writeMessage(outgoing_buffer);
			
			byte[] incoming_buffer = messenger.readMessage();
			var result = Result.Deserialize(incoming_buffer);
			
			return result;
		}

		public Result getUsings()
		{
			StreamedMessageUtils<NetworkStream> messenger = new StreamedMessageUtils<NetworkStream>(this.Client.GetStream());
			
			var request = Request.CreateGetUsingsRequest();
			byte[] outgoing_buffer = request.Serialize();
			messenger.writeMessage(outgoing_buffer);
			
			byte[] incoming_buffer = messenger.readMessage();
			var result = Result.Deserialize(incoming_buffer);
			
			return result;
		}

		#endregion
	}
}

