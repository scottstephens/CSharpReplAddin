// 
// ConsoleView.cs
//  
// Author:
//       Peter Johanson <latexer@gentoo.org>
//       Lluis Sanchez Gual <lluis@novell.com>
//       Scott Stephens <stephens.js@gmail.com>
//
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Scott Stephens (stephens.js@gmail.com)
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
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.CSharpRepl.Components
{
	public class ReplView: ScrolledWindow
	{
		string scriptLines = "";
		
		Stack<string> commandHistoryPast = new Stack<string> ();
		Stack<string> commandHistoryFuture = new Stack<string> ();
		List<Tuple<string,EventHandler>> menuCommands = new List<Tuple<string,EventHandler>>();

		string blockText = "";

		TextView textView;
	
		public ReplView()
		{
			PromptString = "> ";
			PromptMultiLineString = ">> ";
			
			textView = new TextView ();
			Add (textView);
			ShowAll ();
			
			textView.WrapMode = Gtk.WrapMode.Word;
			textView.KeyPressEvent += TextViewKeyPressEvent;
			textView.PopulatePopup += TextViewPopulatePopup;
			
			// The 'Freezer' tag is used to keep everything except
			// the input line from being editable
			TextTag tag = new TextTag ("Freezer");
			tag.Editable = false;
			Buffer.TagTable.Add (tag);
			Prompt (false);
		}
		public void AddMenuCommand(string name, EventHandler handler)
		{
			this.menuCommands.Add(Tuple.Create(name,handler));
		}

		void TextViewPopulatePopup (object o, PopulatePopupArgs args)
		{
			MenuItem item = new MenuItem (GettextCatalog.GetString ("Clear"));
			SeparatorMenuItem sep = new SeparatorMenuItem ();
			
			item.Activated += ClearActivated;
			item.Show ();
			sep.Show ();
			
			args.Menu.Add (sep);
			args.Menu.Add (item);

			foreach (var menu_command in menuCommands)
			{
				var tmp = new MenuItem(menu_command.Item1);
				tmp.Activated += menu_command.Item2;
				tmp.Show();
				args.Menu.Add(tmp);
			}
		}

		void ClearActivated (object sender, EventArgs e)
		{
			Clear ();
		}
		
		public void SetFont (Pango.FontDescription font)
		{
			textView.ModifyFont (font);
		}
		
		public string PromptString { get; set; }
		
		public bool AutoIndent { get; set; }

		public string PromptMultiLineString { get; set; }
		
		[GLib.ConnectBeforeAttribute]
		void TextViewKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = ProcessKeyPressEvent (args.Event);
		}

		void InternalPrompt(bool multiline, bool move_cursor=true)
		{
			promptState = multiline ? PromptState.Multiline : PromptState.Regular;
			if (!multiline)
				blockText = "";

			startOfPrompt = Buffer.CreateMark(null, Buffer.EndIter, true);
		
			TextIter end = Buffer.EndIter;
			if (multiline)
				Buffer.Insert (ref end, PromptMultiLineString);
			else
				Buffer.Insert (ref end, PromptString);


			// Record the end of where we processed, used to calculate start
			// of next input line
			endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true);
			
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);

			if (move_cursor)
			{
				Buffer.PlaceCursor (Buffer.EndIter);
				textView.ScrollMarkOnscreen (Buffer.InsertMark);
			}
		}

		void FinalizeLine(bool move_cursor=true)
		{
			if (this.blockText == "")
				this.blockText += InputLine;
			else 
				this.blockText += Environment.NewLine + InputLine;

			// Everything but the last item (which was input),
			//in the future stack needs to get put back into the
			// past stack
			while (commandHistoryFuture.Count > 1)
				commandHistoryPast.Push (commandHistoryFuture.Pop());
			// Clear the pesky junk input line
			commandHistoryFuture.Clear();
			
			// Record our input line
			commandHistoryPast.Push(InputLine);
			if (scriptLines == "")
				scriptLines += InputLine;
			else
				scriptLines += Environment.NewLine + InputLine;

			Buffer.Insert(Buffer.EndIter, Environment.NewLine);
			startOfPrompt = Buffer.CreateMark(null, Buffer.EndIter, true);
			endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true);
			promptState = PromptState.None;
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);

			if (move_cursor)
			{
				Buffer.PlaceCursor (Buffer.EndIter);
				textView.ScrollMarkOnscreen (Buffer.InsertMark);
				// Freeze all the text except our input line
			}

		}

		void FinalizeBlock()
		{
			this.ProcessInput(blockText);
		}

		void HistoryUp()
		{
			if (commandHistoryPast.Count > 0) {
				if (commandHistoryFuture.Count == 0)
					commandHistoryFuture.Push (InputLine);
				else if (commandHistoryPast.Count > 1 ) 
					commandHistoryFuture.Push (commandHistoryPast.Pop());					
				else 
					return;
				InputLine = commandHistoryPast.Peek();
			}
		}

		void HistoryDown()
		{
			if (commandHistoryFuture.Count > 0) {
				if (commandHistoryFuture.Count == 1)
					InputLine = commandHistoryFuture.Pop();
				else if (commandHistoryFuture.Count > 0) {
					commandHistoryPast.Push (commandHistoryFuture.Pop ());
					InputLine = commandHistoryPast.Peek ();
				}
			}
		}

		bool ProcessKeyPressEvent (Gdk.EventKey ev)
		{
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down)
				return false;
			
			// Needed so people can copy and paste, but always end up
			// typing in the prompt.
			if (Cursor.Compare (InputLineBegin) < 0) {
				Buffer.MoveMark (Buffer.SelectionBound, InputLineEnd);
				Buffer.MoveMark (Buffer.InsertMark, InputLineEnd);
			}
			
//			if (ev.State == Gdk.ModifierType.ControlMask && ev.Key == Gdk.Key.space)
//				TriggerCodeCompletion ();
	
			if (ev.Key == Gdk.Key.Return) {
				this.FinalizeLine();
				this.FinalizeBlock();
				return true;
			}
	
			// The next two cases handle command history	
			else if (ev.Key == Gdk.Key.Up) {
				this.HistoryUp();
				return true;
			}
			else if (ev.Key == Gdk.Key.Down) {
				this.HistoryDown();
				return true;
			}	
			else if (ev.Key == Gdk.Key.Left) {
				// Keep our cursor inside the prompt area
				if (Cursor.Compare (InputLineBegin) <= 0)
					return true;
			}
			else if (ev.Key == Gdk.Key.Home) {
				Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);
				// Move the selection mark too, if shift isn't held
				if ((ev.State & Gdk.ModifierType.ShiftMask) == ev.State)
					Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);
				return true;
			}
			else if (ev.Key == Gdk.Key.period) {
				return false;
			}
	
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			else if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down) {
				return false;
			}
			
			return false;
		}
		
		TextMark endOfLastProcessing;
		TextMark startOfPrompt;
		PromptState promptState;

		public TextIter InputLineBegin {
			get {
				return Buffer.GetIterAtMark (endOfLastProcessing);
			}
		}

		public TextIter InputPromptBegin {
			get {
				return Buffer.GetIterAtMark(startOfPrompt);
			}
		}

		public TextIter InputLineEnd {
			get { return Buffer.EndIter; }
		}
		
		private TextIter Cursor {
			get { return Buffer.GetIterAtMark (Buffer.InsertMark); }
		}
		
		Gtk.TextBuffer Buffer {
			get { return textView.Buffer; }
		}
		
		// The current input line
		public string InputLine {
			get {
				return Buffer.GetText (InputLineBegin, InputLineEnd, false);
			}
			set {
				TextIter start = InputLineBegin;
				TextIter end = InputLineEnd;
				Buffer.Delete (ref start, ref end);
				start = InputLineBegin;
				Buffer.Insert (ref start, value);
			}
		}

		protected virtual void ProcessInput (string line)
		{
			if (ConsoleInput != null)
				ConsoleInput (this, new ConsoleInputEventArgs (line));
		}

		public void WriteOutput (string line)
		{
			string line_in_progress = this.InputLine;
			TextIter start = this.InputPromptBegin;
			TextIter end = this.InputLineEnd;
			Buffer.Delete(ref start, ref end);
			start = this.InputPromptBegin;
			Buffer.Insert(ref start, line);

			if (promptState != PromptState.None)
				this.Prompt(!line.EndsWith(Environment.NewLine), promptState == PromptState.Multiline );
			start = this.InputLineBegin;
			Buffer.Insert(ref start, line_in_progress);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
		}
	
		protected void WriteNonTerminatedInput(string content)
		{
			TextIter start = Buffer.EndIter;
			Buffer.Insert (ref start , content);
		}
		public void WriteInput(string content)
		{
			string[] lines = content.Split(new string[]{Environment.NewLine},StringSplitOptions.RemoveEmptyEntries);

			for (int ii = 0; ii < lines.Length; ++ii)
			{
				if (ii > 0) {
					this.InternalPrompt(true, false);
				}
				this.WriteNonTerminatedInput(lines[ii]);
				this.FinalizeLine(ii == lines.Length - 1);
			}
			this.FinalizeBlock();
		}

		public void Prompt (bool newLine)
		{
			Prompt (newLine, false);
		}
	
		public void Prompt (bool newLine, bool multiline)
		{
			TextIter end = Buffer.EndIter;
			if (newLine)
				Buffer.Insert (ref end, "\n");

			this.InternalPrompt(multiline);
		}
		
		public void Clear ()
		{
			Buffer.Text = "";
			scriptLines = "";
			Prompt (false);
		}
		
		public void ClearHistory ()
		{
			commandHistoryFuture.Clear ();
			commandHistoryPast.Clear ();
		}
		
		public string BlockStart { get; set; }
		
		public string BlockEnd { get; set; }
		
		public event EventHandler<ConsoleInputEventArgs> ConsoleInput;
	}

	public enum PromptState { None, Regular, Multiline }

	public class ConsoleInputEventArgs: EventArgs
	{
		public ConsoleInputEventArgs (string text)
		{
			Text = text;
		}
		
		public string Text { get; internal set; }
	}
}
