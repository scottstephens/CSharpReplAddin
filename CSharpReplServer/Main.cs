using System;
using MonoDevelop.CSharpRepl;

namespace CSharpShellServer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			MonoDevelop.CSharpRepl.CSharpReplServer.Run(args);
		}
	}
}
