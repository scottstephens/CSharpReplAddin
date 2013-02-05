using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using Mono.CSharp;

namespace MonoDevelop.CSharpRepl
{
	public class CSharpReplServer
	{
		private TcpListener Listener { get; set; }
		private Thread ListenThread { get; set; }
		private CSharpRepl Repl { get; set; }

		public CSharpReplServer (int port)
		{
			this.Listener = new TcpListener(IPAddress.Loopback, port);
			this.ListenThread = new Thread(new ThreadStart(listenForClients));
			this.Repl = new CSharpRepl();
		}

		public void Start()
		{

			this.ListenThread.Start();
		}

		private void listenForClients()
		{
			this.Listener.Start();
			
			while (true)
			{
				//blocks until a client has connected to the server
				TcpClient client = this.Listener.AcceptTcpClient();
				
				//create a thread to handle communication
				//with connected client
				Thread clientThread = new Thread(new ParameterizedThreadStart(handleClientComm));
				clientThread.Start(client);
			}
		}

		private void handleClientComm(object client)
		{
			TcpClient tcpClient = (TcpClient)client;

			var messenger = new StreamedMessageUtils<NetworkStream>(tcpClient.GetStream());
			
			while (true)
			{
				
				try
				{
					//blocks until a client sends a message
					byte[] buffer = messenger.readMessage();
					var request = CSharpReplEvaluationRequest.Deserialize(buffer);
					CSharpReplEvaluationResult result = this.Repl.evaluate(request.Code);
					byte[] output_buffer = result.Serialize();
					messenger.writeMessage(output_buffer);
				}
				catch (Exception e)
				{
					//an error has occured
					break;
				}
				Console.Out.Flush();
			}

			tcpClient.Close();
		}

		public static void Run(string[] args)
		{
			var server = new CSharpReplServer(Int32.Parse(args[0]));
			server.Start();
		}
	}
}

