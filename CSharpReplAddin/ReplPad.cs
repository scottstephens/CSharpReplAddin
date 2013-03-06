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
using MonoDevelop.Projects;

namespace MonoDevelop.CSharpRepl
{
	public class ReplPad: IPadContent
	{
		public static ReplPad Instance = null;

		public bool Running { get; private set; }

		Pango.FontDescription customFont;
		ReplView view;
		bool disposed;
		ICSharpRepl shell;

		Process _repl_process;
		StreamOutputter _stdout;
		StreamOutputter _stderr;

		public void Initialize (IPadWindow container)
		{
			if (IdeApp.Preferences.CustomOutputPadFont != null)
				customFont = IdeApp.Preferences.CustomOutputPadFont;
			else 
				customFont = Pango.FontDescription.FromString("Courier New");
			
			view = new ReplView ();
			view.PromptMultiLineString = "+ ";
			view.ConsoleInput += OnViewConsoleInput;
			view.SetFont (customFont);
			view.ShadowType = Gtk.ShadowType.None;
			//view.AddMenuCommand("Start Interactive Session", StartInteractiveSessionHandler);
			//view.AddMenuCommand("Connect to Interactive Session", ConnectToInteractiveSessionHandler);
			view.ShowAll ();
			
			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomOutputPadFontChanged;

			ReplPad.Instance = this;
		}

		public void Start()
		{
			// Start Repl process
			if (!this.Running)
			{
				this.StartInteractiveSession();
				this.ConnectToInteractiveSession();
				this.Running = true;
			}
		}

		public void Stop()
		{
			if (_stderr != null) {
				_stderr.Stop(); 
				_stderr = null;
			}
			if (_stdout != null) {
				_stdout.Stop();
				_stdout = null;
			}
			if (_repl_process != null)
			{
				try
				{
					_repl_process.Kill();
				}
				catch (InvalidOperationException)
				{
				}
				_repl_process.Close();
				_repl_process.Dispose();
				_repl_process = null;
			}
			this.Running = false;
			view.WriteOutput("Disconnected.");
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
			//string  framework_exe = @"C:\Program Files (x86)\Mono-2.10.9\bin\mono.exe";
			//var start_info = new ProcessStartInfo(framework_exe, repl_exe + " 33333");
			var start_info = new ProcessStartInfo(repl_exe,"33333");
			start_info.UseShellExecute = false;
			start_info.CreateNoWindow = true;
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
			
			customFont = IdeApp.Preferences.CustomOutputPadFont;
			
			view.SetFont (customFont);
		}

		public void InputBlock(string block, string prefix_to_strip="")
		{
			this.view.WriteInput(block, prefix_to_strip);
		}

		public void LoadReferences(DotNetProject project)
		{
			foreach ( var x in project.References)
			{
				if (x.ReferenceType == ReferenceType.Assembly) {
					// Just a path to the reference, can be passed in no problem
					this.shell.loadAssembly(x.Reference);
				} else if (x.ReferenceType == ReferenceType.Gac || x.ReferenceType == ReferenceType.Package) {
					// The fully-qualified name of the assembly, can be passed in no problem
					this.shell.loadAssembly(x.Reference);
				} else if (x.ReferenceType == ReferenceType.Project) {
					DotNetProject inner_project = project.ParentSolution.FindProjectByName(x.Reference) as DotNetProject;
					if (inner_project != null) {
						var config = inner_project.GetConfiguration(IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						string file_name = config.CompiledOutputName.FullPath.ToString();
						this.shell.loadAssembly(file_name);
					} else 
						this.view.WriteOutput(String.Format ("Cannot load non .NET project reference: {0}/{1}", project.Name, x.Reference));
				}
			}
		}


		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (this.shell == null)
			{
				this.view.WriteOutput("Not connected.");
				this.view.Prompt(true);
				return;
			}
			
			Result result;
			try {
				result = this.shell.evaluate(e.Text);
			} catch (Exception ex) {
				view.WriteOutput("Evaluation failed: " + ex.Message);
				view.Prompt(true);
				return;
			}
			
			switch (result.Type)
			{
			case ResultType.FAILED:
				view.WriteOutput(result.ResultMessage);
				view.Prompt(false);
				break;
			case ResultType.NEED_MORE_INPUT:
				view.Prompt (false,true);
				break;
			case ResultType.SUCCESS_NO_OUTPUT:
				view.Prompt(false);
				break;
			case ResultType.SUCCESS_WITH_OUTPUT:
				view.WriteOutput(result.ResultMessage);	
				view.Prompt(true);
				break;
			default:
				throw new Exception("Unexpected state! Contact developers.");
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
				this.Stop();

				IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomOutputPadFontChanged;
				if (customFont != null)
					customFont.Dispose ();

				disposed = true;
			}
		}
	}


}