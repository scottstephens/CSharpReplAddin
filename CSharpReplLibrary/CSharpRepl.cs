using System;
using System.IO;
using System.Collections;
using Mono.CSharp;

namespace MonoDevelop.CSharpRepl
{

	public interface ICSharpRepl 
	{
		Result evaluate(string input);
		Result loadAssembly(string file);
		Result getVariables();
		Result getUsings();
	}

	public class CSharpRepl : ICSharpRepl
	{
		private Evaluator InnerEvaluator { get; set; }
		private CompilerSettings Settings { get; set; }
		private CompilerContext Context { get; set; }
		private StreamReportPrinter Printer { get; set; }
		private StringWriter CompilerErrorWriter { get; set; }

		public CSharpRepl ()
		{
			this.Settings = new CompilerSettings();
			this.CompilerErrorWriter = new StringWriter();
			this.Printer = new StreamReportPrinter(this.CompilerErrorWriter);
			this.Context = new CompilerContext (this.Settings, this.Printer);
			this.InnerEvaluator = new Evaluator(this.Context);
			this.InnerEvaluator.InteractiveBaseClass = typeof(InteractiveBase);
			this.InnerEvaluator.DescribeTypeExpressions = true;
			this.InitializeUsings();
		}

		private void InitializeUsings()
		{
			this.evaluate("using System; using System.Linq; using System.Collections.Generic; using System.Collections;");
		}

		public Result evaluate(string input)
		{
			Result output;
			try 
			{
				object result;
				bool result_set;
				input = this.InnerEvaluator.Evaluate (input, out result, out result_set);

				if (this.Printer.ErrorsCount > 0)
				{
					output = new Result(ResultType.FAILED, this.CompilerErrorWriter.GetStringBuilder().ToString());
					this.CompilerErrorWriter.GetStringBuilder().Clear();
				}
				else if (result_set)
				{
					StringWriter output_writer = new StringWriter();
					PrettyPrint (output_writer, result);
					output_writer.Close();
					output = new Result(ResultType.SUCCESS_WITH_OUTPUT, output_writer.GetStringBuilder().ToString());
				} 
				else if (input == null)
				{
					output = new Result(ResultType.SUCCESS_NO_OUTPUT, null);
				}
				else 
				{
					output = new Result(ResultType.NEED_MORE_INPUT, null);
				}

			} 
			catch (Exception e)
			{
				output = new Result(ResultType.FAILED, e.ToString());
			}
			this.Printer.Reset();
			return output;
		}

		public Result loadAssembly(string name_or_path)
		{
			try {
				this.InnerEvaluator.LoadAssembly(name_or_path);
				return new Result(ResultType.SUCCESS_NO_OUTPUT, null);
			} catch(Exception e) {
				return new Result(ResultType.FAILED,e.Message);
			}
		}
		public Result loadPackage(string package)
		{
			try {
				InteractiveBase.LoadPackage(package);
				return new Result(ResultType.SUCCESS_NO_OUTPUT, null);
			} catch (Exception e) {
				return new Result(ResultType.FAILED, e.Message);
			}
		}

		public Result getVariables()
		{
			try {
				string vars = this.InnerEvaluator.GetVars();
				return new Result(ResultType.SUCCESS_WITH_OUTPUT, vars);
			} catch (Exception e) {
				return new Result(ResultType.FAILED, e.Message);
			}
		}

		public Result getUsings()
		{
			try {
				string usings = this.InnerEvaluator.GetUsing();
				return new Result(ResultType.SUCCESS_WITH_OUTPUT, usings);
			} catch (Exception e) {
				return new Result(ResultType.FAILED, e.Message);
			}
		}

		#region pretty printing
		static void p (TextWriter output, string s)
		{
			output.Write (s);
		}
		
		static string EscapeString (string s)
		{
			return s.Replace ("\"", "\\\"");
		}
		
		static void EscapeChar (TextWriter output, char c)
		{
			if (c == '\''){
				output.Write ("'\\''");
				return;
			}
			if (c > 32){
				output.Write ("'{0}'", c);
				return;
			}
			switch (c){
			case '\a':
				output.Write ("'\\a'");
				break;
				
			case '\b':
				output.Write ("'\\b'");
				break;
				
			case '\n':
				output.Write ("'\\n'");
				break;
				
			case '\v':
				output.Write ("'\\v'");
				break;
				
			case '\r':
				output.Write ("'\\r'");
				break;
				
			case '\f':
				output.Write ("'\\f'");
				break;
				
			case '\t':
				output.Write ("'\\t");
				break;
				
			default:
				output.Write ("'\\x{0:x}", (int) c);
				break;
			}
		}
		
		// Some types (System.Json.JsonPrimitive) implement
		// IEnumerator and yet, throw an exception when we
		// try to use them, helper function to check for that
		// condition
		static internal bool WorksAsEnumerable (object obj)
		{
			IEnumerable enumerable = obj as IEnumerable;
			if (enumerable != null){
				try {
					enumerable.GetEnumerator ();
					return true;
				} catch {
					// nothing, we return false below
				}
			}
			return false;
		}

		internal static void PrettyPrint (TextWriter output, object result)
		{
			if (result == null){
				p (output, "null");
				return;
			}
			
			if (result is Array){
				Array a = (Array) result;
				
				p (output, "{ ");
				int top = a.GetUpperBound (0);
				for (int i = a.GetLowerBound (0); i <= top; i++){
					PrettyPrint (output, a.GetValue (i));
					if (i != top)
						p (output, ", ");
				}
				p (output, " }");
			} else if (result is bool){
				if ((bool) result)
					p (output, "true");
				else
					p (output, "false");
			} else if (result is string){
				p (output, String.Format ("\"{0}\"", EscapeString ((string)result)));
			} else if (result is IDictionary){
				IDictionary dict = (IDictionary) result;
				int top = dict.Count, count = 0;
				
				p (output, "{");
				foreach (DictionaryEntry entry in dict){
					count++;
					p (output, "{ ");
					PrettyPrint (output, entry.Key);
					p (output, ", ");
					PrettyPrint (output, entry.Value);
					if (count != top)
						p (output, " }, ");
					else
						p (output, " }");
				}
				p (output, "}");
			} else if (WorksAsEnumerable (result)) {
				int i = 0;
				p (output, "{ ");
				foreach (object item in (IEnumerable) result) {
					if (i++ != 0)
						p (output, ", ");
					
					PrettyPrint (output, item);
				}
				p (output, " }");
			} else if (result is char) {
				EscapeChar (output, (char) result);
			} else {
				p (output, result.ToString ());
			}
		}
		#endregion 
	}

	

}

