using System;
using System.Linq;
using MonoDevelop.Components.Commands;
using MonoDevelop.CSharpRepl;
using ExtensibleTextEditor = MonoDevelop.SourceEditor.ExtensibleTextEditor;
using DocumentLine = Mono.TextEditor.DocumentLine;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Text;

namespace MonoDevelop.CSharpRepl.Commands
{
	public enum ReplCommands { RunSelection, RunLine, LaunchReplForActiveDocument, LaunchGenericRepl, StopRepl }

	internal static class CommandHelper 
	{
		public static bool ActiveDocumentIsCSharp { get {
				bool no_nulls = IdeApp.Workbench != null && IdeApp.Workbench.ActiveDocument != null;
				DotNetProject project = no_nulls ? IdeApp.Workbench.ActiveDocument.Project as DotNetProject : null;
				return project != null && project.LanguageName == "C#";
			}
		}

		public static string SpacesAtStart(string input) {
			return input.Substring(0,input.Length - input.TrimStart().Length);
		}
	}

	public class RunSelectionHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.RootWindow.HasToplevelFocus) {
				ExtensibleTextEditor editor = IdeApp.Workbench.RootWindow.Focus as ExtensibleTextEditor;
				if (editor != null) {
					string spaces_at_start = CommandHelper.SpacesAtStart(editor.GetTextAt(editor.SelectedLines.First().Segment));
					StringBuilder block_builder = new StringBuilder();
					if (editor.SelectedText == null)
					{
						foreach (var line in editor.SelectedLines)
						{
							string x = editor.GetTextAt(line.Segment);
							block_builder.AppendLine(x);
						}
						ReplPad.Instance.InputBlock(block_builder.ToString(), spaces_at_start);
					} else {
						ReplPad.Instance.InputBlock(editor.SelectedText, spaces_at_start);
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
					string spaces_at_start = CommandHelper.SpacesAtStart(editor.GetTextAt(editor.SelectedLines.First().Segment));
					StringBuilder block_builder = new StringBuilder();
					foreach (var line in editor.SelectedLines)
					{
						string x = editor.GetTextAt(line.Segment);
						block_builder.AppendLine(x);
						last_line = line;
					}
					ReplPad.Instance.InputBlock(block_builder.ToString(), spaces_at_start);
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
				ReplPad.Instance.Start();
				ReplPad.Instance.LoadReferences(project);
			}
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = !ReplPad.Instance.Running;
			info.Visible = CommandHelper.ActiveDocumentIsCSharp;
		}
	}

	public class LaunchGenericReplHandler : CommandHandler 
	{
		protected override void Run()
		{
			ReplPad.Instance.Start();
		}
		protected override void Update (CommandInfo info)
		{
			info.Enabled = !ReplPad.Instance.Running;
			info.Visible = true;
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
			info.Visible = true;
			info.Enabled = ReplPad.Instance != null && ReplPad.Instance.Running;
		}

	}
}

