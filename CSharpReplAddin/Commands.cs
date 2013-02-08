using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.CSharpRepl;
using ExtensibleTextEditor = MonoDevelop.SourceEditor.ExtensibleTextEditor;
using DocumentLine = Mono.TextEditor.DocumentLine;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharpRepl.Commands
{
	public enum ReplCommands { RunSelection, RunLine, LaunchReplForActiveDocument, StopRepl }

	internal static class CommandHelper 
	{
		public static bool ActiveDocumentIsCSharp { get {
				bool no_nulls = IdeApp.Workbench != null && IdeApp.Workbench.ActiveDocument != null;
				DotNetProject project = no_nulls ? IdeApp.Workbench.ActiveDocument.Project as DotNetProject : null;
				return project != null && project.LanguageName == "C#";
			}
		}
	}

	public class RunSelectionHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				ExtensibleTextEditor editor = IdeApp.Workbench.RootWindow.Focus as ExtensibleTextEditor;
				if (editor != null) {
					foreach (var line in editor.SelectedLines)
					{
						string x = editor.GetTextAt(line.Segment);
						ReplPad.Instance.InputLine(x);
					}
				}
			}
		}
		protected override void Update (CommandInfo info)
		{
			info.Visible = CommandHelper.ActiveDocumentIsCSharp;
			info.Enabled = CommandHelper.ActiveDocumentIsCSharp && ReplPad.Instance.Running;
		}
	}

	public class RunLineHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				ExtensibleTextEditor editor = IdeApp.Workbench.RootWindow.Focus as ExtensibleTextEditor;

				DocumentLine last_line = null;
				if (editor != null) {
					foreach (var line in editor.SelectedLines)
					{
						string x = editor.GetTextAt(line.Segment);
						ReplPad.Instance.InputLine(x);
						last_line = line;
					}
					editor.ClearSelection();
					editor.Caret.Line = last_line.LineNumber + 1;
				}
			}
		}
		protected override void Update (CommandInfo info)
		{
			info.Visible = CommandHelper.ActiveDocumentIsCSharp;
			info.Enabled = CommandHelper.ActiveDocumentIsCSharp && ReplPad.Instance.Running;
		}
	}

	public class LaunchReplForActiveDocumentHandler : CommandHandler 
	{
		protected override void Run()
		{
			DotNetProject project = IdeApp.Workbench.ActiveDocument.Project as DotNetProject;

			if (project != null)
			{
				foreach ( var x in project.References)
				{
					string y = x.Reference;
					string z = x.ReferenceType.ToString();
				}
				ReplPad.Instance.Start();
			}
		}

		protected override void Update (CommandInfo info)
		{
			bool no_nulls = IdeApp.Workbench != null && IdeApp.Workbench.ActiveDocument != null;
			DotNetProject project = no_nulls ? IdeApp.Workbench.ActiveDocument.Project as DotNetProject : null;
			info.Enabled = project != null && project.LanguageName == "C#";
		}
	}

	public class StopReplHandler : CommandHandler
	{
		protected override void Run ()
		{
			ReplPad.Instance.Stop();
		}

		protected override void Update(CommandInfo info)
		{
			if (ReplPad.Instance != null && ReplPad.Instance.Running)
			{
				info.Visible = true;
				info.Enabled = true;
				return;
			} else if (CommandHelper.ActiveDocumentIsCSharp) {
				info.Visible = true;
				info.Enabled = false;
			} else {
				info.Visible = false;
				info.Enabled = false;
			}
		}

	}
}

