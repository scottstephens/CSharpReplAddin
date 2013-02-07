using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.CSharpRepl;
using ExtensibleTextEditor = MonoDevelop.SourceEditor.ExtensibleTextEditor;
using DocumentLine = Mono.TextEditor.DocumentLine;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharpRepl.Commands
{
	public enum ReplCommands { RunSelection, RunLine }

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
	}
}

