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
		private int Port { get; set; }
		private TcpListener Listener { get; set; }
		private Thread ListenThread { get; set; }
		private CSharpRepl Repl { get; set; }

		public CSharpReplServer (int port)
		{
			this.Port = port;
		}

		public void Start()
		{
			this.Listener = new TcpListener(IPAddress.Loopback, this.Port);
			this.ListenThread = new Thread(new ThreadStart(listenForClients));
			this.Repl = new CSharpRepl();
			this.ListenThread.Start();
		}

		private void listenForClients()
		{
			try {
				this.Listener.Start();
			} catch (Exception e) {
				Console.WriteLine("Error starting TCP listener: " + e.Message);
				return;
			}
			
			while (true)
			{
				//blocks until a client has connected to the server
				TcpClient client;
				try {
					client = this.Listener.AcceptTcpClient();
				} catch (Exception e) {
					Console.WriteLine("Error accepting client connection: " + e.Message);
					break;
				}
				
				//handle communications with connected client
				try {
					handleClientComm(client);
				} catch (Exception e) {
					Console.WriteLine("Error in handling client communications: " + e.Message);
					break;
				}
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
					var request = Request.Deserialize(buffer);
					Result result;
					if (request.Type == RequestType.Evaluate) {
						result = this.Repl.evaluate(request.Code);
					} else if (request.Type == RequestType.LoadAssembly) {
						result = this.Repl.loadAssembly(request.AssemblyToLoad);
					} else if (request.Type == RequestType.Variables) {
						result = this.Repl.getVariables();
					} else if (request.Type == RequestType.Usings) {
						result = this.Repl.getUsings();
					} else {
						Console.WriteLine("Received unexpected request type {0}",request.Type);
						break;
					}

					byte[] output_buffer = result.Serialize();
					messenger.writeMessage(output_buffer);
				}
				catch (Exception e)
				{
					Console.WriteLine("Unexpected error occurred: " + e.Message);
					break;
				}
				Console.Out.Flush();
				Console.Error.Flush();
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

