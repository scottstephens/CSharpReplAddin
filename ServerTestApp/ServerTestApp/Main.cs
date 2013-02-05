using System;
using MonoDevelop.CSharpRepl;

namespace ServerTestApp
{
	public class MainClass
	{
		public static void Main (string[] args)
		{
			var proxy = new CSharpReplServerProxy(33333);
			proxy.Start();
			var output = proxy.evaluate("var x = 10;");
//			var repl = new CSharpRepl();
//			var output = repl.evaluate("10*10;");
			Console.WriteLine(output.ResultType.ToString());
//			output = repl.evaluate("var x = 10;");
//			Console.WriteLine("{0}: {1}",output.ResultType.ToString(),output.Result);
//			output = repl.evaluate("x;");
//			Console.WriteLine("{0}: {1}",output.ResultType.ToString(),output.Result);
		}
	}
}
