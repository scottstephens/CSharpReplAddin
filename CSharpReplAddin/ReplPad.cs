// 
// ReplPad.cs
//  
// Author:
//       Scott Stephens <stephens.js@gmail.com>
// 
// Copyright (c) 2012 Scott Stephens
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Assembly = System.Reflection.Assembly;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharpRepl.Components;

namespace MonoDevelop.CSharpRepl
{
	public class ReplPad: IPadContent
	{
		public static ReplPad Instance = null;

		Pango.FontDescription customFont;
		ReplView view;
		bool disposed;
		ICSharpRepl shell;
		string commandInProgress = null;

		Process _repl_process;
		StreamOutputter _stdout;
		StreamOutputter _stderr;

		public void Initialize (IPadWindow container)
		{
			if (IdeApp.Preferences.CustomOutputPadFont != null)
				customFont = Pango.FontDescription.FromString (IdeApp.Preferences.CustomOutputPadFont);
			else 
				customFont = Pango.FontDescription.FromString("Courier New");
			
			view = new ReplView ();
			view.PromptMultiLineString = "+ ";
			view.ConsoleInput += OnViewConsoleInput;
			view.SetFont (customFont);
			view.ShadowType = Gtk.ShadowType.None;
			view.AddMenuCommand("Start Interactive Session", StartInteractiveSessionHandler);
			view.AddMenuCommand("Connect to Interactive Session", ConnectToInteractiveSessionHandler);
			view.ShowAll ();
			
			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomOutputPadFontChanged;

			// Start Repl process
			this.StartInteractiveSession();
			this.ConnectToInteractiveSession();
			ReplPad.Instance = this;
		}
		void StartInteractiveSessionHandler(object sender, EventArgs e)
        {
			this.StartInteractiveSession();
		}
		void ConnectToInteractiveSessionHandler(object sender, EventArgs e)
		{
			ConnectToInteractiveSession();
		}
		void StartInteractiveSession()
		{
			string bin_dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ReplPad)).Location);
			string repl_exe = Path.Combine(bin_dir, "CSharpReplServer.exe");
			var start_info = new ProcessStartInfo(repl_exe,"33333");
			start_info.UseShellExecute = false;
			start_info.RedirectStandardError = true;
			start_info.RedirectStandardOutput = true;
			
			_repl_process = Process.Start(start_info);
			_stdout = new StreamOutputter(_repl_process.StandardOutput, view);
			_stderr = new StreamOutputter(_repl_process.StandardError, view);
			_stdout.Start();
			_stderr.Start();
			Thread.Sleep(1000); // Give _repl_process time to start up before we let anybody do anything with it
		}
		void ConnectToInteractiveSession()
		{
			var tmpshell = new CSharpReplServerProxy(33333);
			try {
				tmpshell.Start();
				this.shell = tmpshell;
			} catch (Exception e) {
				this.shell = null;
				view.WriteOutput("Failed connecting to interactive session: " + e.Message);
			}
		}
		
		void HandleCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			if (customFont != null) {
				customFont.Dispose ();
				customFont = null;
			}
			
			customFont = Pango.FontDescription.FromString (IdeApp.Preferences.CustomOutputPadFont);
			
			view.SetFont (customFont);
		}

		public void InputLine(string line)
		{
			this.view.WriteOutput(line+Environment.NewLine);
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (this.shell == null)
			{
				this.view.WriteOutput("Not connected.");
				this.view.Prompt(true);
				return;
			}

			if (this.commandInProgress != null)
				this.commandInProgress += "\n" + e.Text;
			else 
				this.commandInProgress = e.Text;

			CSharpReplEvaluationResult result;
			try {
				result = this.shell.evaluate(this.commandInProgress);
			} catch (Exception ex) {
				view.WriteOutput("Evaluation failed: " + ex.Message);
				view.Prompt(true);
				return;
			}

			lock(view)
			{
				switch (result.ResultType)
				{
					case CSharpReplEvaluationResultType.FAILED:
						view.WriteOutput(result.Result);
						this.commandInProgress = null;
						view.Prompt(false);
						break;
					case CSharpReplEvaluationResultType.NEED_MORE_INPUT:
						view.Prompt (false,true);
						break;
					case CSharpReplEvaluationResultType.SUCCESS_NO_OUTPUT:
						this.commandInProgress = null;
						view.Prompt(false);
						break;
					case CSharpReplEvaluationResultType.SUCCESS_WITH_OUTPUT:
						view.WriteOutput(result.Result);	
						view.Prompt(true);						
						this.commandInProgress = null;						
						break;
					default:
						throw new Exception("Unexpected state! Contact developers.");
				}
			}
		}
		
//		void PrintValue (ObjectValue val)
//		{
//			string result = val.Value;
//			if (string.IsNullOrEmpty (result)) {
//				if (val.IsNotSupported)
//					result = GettextCatalog.GetString ("Expression not supported.");
//				else if (val.IsError || val.IsUnknown)
//					result = GettextCatalog.GetString ("Evaluation failed.");
//				else
//					result = string.Empty;
//			}
//			view.WriteOutput (result);
//		}
//		
//		void WaitForCompleted (ObjectValue val)
//		{
//			int iteration = 0;
//			
//			GLib.Timeout.Add (100, delegate {
//				if (!val.IsEvaluating) {
//					if (iteration >= 5)
//						view.WriteOutput ("\n");
//					PrintValue (val);
//					view.Prompt (true);
//					return false;
//				}
//				if (++iteration == 5)
//					view.WriteOutput (GettextCatalog.GetString ("Evaluating") + " ");
//				else if (iteration > 5 && (iteration - 5) % 10 == 0)
//					view.WriteOutput (".");
//				else if (iteration > 300) {
//					view.WriteOutput ("\n" + GettextCatalog.GetString ("Timed out."));
//					view.Prompt (true);
//					return false;
//				}
//				return true;
//			});
//		}
		
		public void RedrawContent ()
		{
		}
		
		public Gtk.Widget Control {
			get {
				return view;
			}
		}
		
		public void Dispose ()
		{
			if (!disposed) {
				IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomOutputPadFontChanged;
				if (customFont != null)
					customFont.Dispose ();
				if (_stderr != null)
					_stderr.Stop(); 
				if (_stdout != null)
					_stdout.Stop();
				if (_repl_process != null)
				{
					_repl_process.Kill();
					_repl_process.Close();
					_repl_process.Dispose();
				}
				disposed = true;
			}
		}
	}


}