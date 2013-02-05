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

		public CSharpReplEvaluationResult evaluate (string input)
		{
			StreamedMessageUtils<NetworkStream> messenger = new StreamedMessageUtils<NetworkStream>(this.Client.GetStream());

			var request = new CSharpReplEvaluationRequest(input);
			byte[] outgoing_buffer = request.Serialize();
			messenger.writeMessage(outgoing_buffer);

			byte[] incoming_buffer = messenger.readMessage();
			var result = CSharpReplEvaluationResult.Deserialize(incoming_buffer);

			return result;
		}

		#endregion
	}
}

