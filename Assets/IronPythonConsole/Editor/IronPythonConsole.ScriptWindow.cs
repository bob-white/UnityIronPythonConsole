/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace IronPythonConsole
{
    public partial class IPyWindow : EditorWindow
    {
        // Script window is more complex.
        // It needs to handle various amounts of user input.
        // It needs to have two text areas, one for editting, and one for display.
        // Just in case I ever get syntax Highlighting working in a non-crashtastic manner.
        // It also needs the lineCount text area.
        private partial class ScriptWindow
        {
            public string text { get { return content.text; } }
            public Rect position = new Rect();

            public ScriptWindow(IPyWindow parent)
            {
                // The edit window should have the same style as the rest of our textAreas
                // Except it should render clear.
                editTextAreaStyle = new GUIStyle(parent.skin.textArea);
                editWindowStyle = new GUIStyle(parent.skin.window);
                editBackground = parent.skin.textArea.normal.background;
                editBackground.SetPixels(0, 0, 8, 8, new List<Color>(from x in Enumerable.Range(0, 64) select Color.clear).ToArray());
                editBackground.Apply();
                editTextAreaStyle.normal.background = editWindowStyle.normal.background = editBackground;
                editTextAreaStyle.normal.textColor = editWindowStyle.normal.textColor = Color.clear;
                this.parent = parent;
            }

            public void Update()
            {
                Rect editor = new Rect(lineCountWidth, position.y, position.width - lineCountWidth, position.height);
                Rect lineCount = new Rect(position.x, position.y, lineCountWidth, position.height);
                GUI.Window(10, editor, EditWindow, "", editWindowStyle);
                GUI.Window(11, editor, DisplayWindow, "");
                GUI.Window(12, lineCount, LineCount, "");
            }

            private IPyWindow parent;
            private TextEditor te;
            private Texture2D editBackground;
            private GUIStyle editTextAreaStyle;
            private GUIStyle editWindowStyle;
            private GUIContent content = new GUIContent();
            private Vector2 scrollView = new Vector2();
            private float lineCountWidth = 0f;

            private void EditWindow(int id)
            {
                // Event detection needs to happen before the textarea is drawn.
                // Otherwise it ends up eating the input events, and we just get
                // Event.current.type == EventType.Used
                // Which helps nobody.
                HandleEvents(Event.current);
                scrollView = GUILayout.BeginScrollView(scrollView);
                content.text = GUILayout.TextArea(content.text, editTextAreaStyle);
                te = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
                GUI.BringWindowToFront(id);
                GUILayout.EndScrollView();
            }

            private void DisplayWindow(int id)
            {
                GUILayout.BeginScrollView(scrollView);
                GUILayout.TextArea(text);
                GUI.BringWindowToBack(id);
                GUILayout.EndScrollView();
            }

            private void LineCount(int id)
            {
                int lineCount = content.text.Split('\n').Length;
                int linePad = (int)Mathf.Log10((float)lineCount) + 1;
                List<string> nums = new List<string>(from x in Enumerable.Range(0, lineCount)
                                                     select x.ToString().PadLeft(linePad));
                string lines = string.Join("\n", nums.ToArray());
                lineCountWidth = GUI.skin.window.CalcSize(new GUIContent(lines)).x;
                GUILayout.BeginScrollView(scrollView, GUIStyle.none, GUIStyle.none);
                GUI.enabled = false;
                GUILayout.TextArea(lines);
                GUI.enabled = true;
                GUILayout.EndScrollView();
            }

            private void HandleEvents(Event current)
            {

                switch (current.type)
                {
                    case EventType.ValidateCommand:
                        switch (current.commandName)
                        {
                            case "Cut":
                                te.Cut();
                                content.text = te.content.text;
                                current.Use();
                                break;
                            case "Copy":
                                te.Copy();
                                current.Use();
                                break;
                            case "Paste":
                                te.Paste();
                                content.text = te.content.text;
                                current.Use();
                                break;
                            case "SelectAll":
                                te.SelectAll();
                                current.Use();
                                break;

                        }
                        break;
                    case EventType.keyUp:
                        switch (current.keyCode)
                        {
                            case KeyCode.Tab:
                                // Haven't figured out a way around Ctrl+Tab changing window focus.
                                // So we just ignore it.
                                if (Event.current.control)
                                    return;
                                if (Event.current.shift)
                                    HandleShiftTab(te);
                                else
                                    HandleTab(te);
                                current.Use();
                                break;
                        }
                        break;
                    case EventType.keyDown:
                        if (current.character == '\n' &&
                            current.control)
                        {
                            if (!EditorApplication.isPlaying)
                            {
                                parent.Interpret();
                            }
                            current.Use();
                        }
                        break;

                }
            }

            private void HandleTab(TextEditor te)
            {
                // If nothing is selected, just insert a tab.
                // Otherwise shift the block to the right.
                if (te.pos == te.selectPos)
                {
                    InsertTab(te);
                }
                else
                {
                    ShiftTextBlockRight(te);
                }
            }

            private void InsertTab(TextEditor te)
            {
                te.ReplaceSelection(parent.spaceTab);
                content.text = te.content.text;
            }

            private void ShiftTextBlockRight(TextEditor te)
            {
                AdjustSelectPos(te);
                int startPos = te.pos;
                int startSelectPos = te.selectPos;
                te.ExpandSelectGraphicalLineStart();
                te.ExpandSelectGraphicalLineEnd();
                string[] lines = te.SelectedText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = parent.spaceTab + lines[i];
                }
                te.ReplaceSelection(string.Join("\n", lines));
                content.text = te.content.text;
                te.selectPos = startSelectPos + parent.spaceTab.Length;
                te.pos = startPos + parent.spaceTab.Length * lines.Length;
            }

            private void HandleShiftTab(TextEditor te)
            {
                if (te.pos == te.selectPos)
                {
                    RemoveTab(te);
                }
                else
                {
                    ShiftTextBlockLeft(te);
                }
            }

            private void RemoveTab(TextEditor te)
            {
                int i = 0;
                int pos = te.pos;
                for (; i < parent.spaceTab.Length; i++)
                {
                    if (pos - i == 0 || te.content.text[pos - (i + 1)] != ' ')
                        break;
                    te.SelectLeft();
                }
                te.ReplaceSelection("");
                content.text = te.content.text;
            }

            private void ShiftTextBlockLeft(TextEditor te)
            {
                AdjustSelectPos(te);
                int AdjustedLines = 0;
                int startPos = te.pos;
                int startSelectPos = te.selectPos;
                te.ExpandSelectGraphicalLineStart();
                te.ExpandSelectGraphicalLineEnd();
                int lineStartPos = te.selectPos;
                string[] lines = te.SelectedText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(parent.spaceTab))
                    {
                        lines[i] = lines[i].Substring(parent.spaceTab.Length);
                        AdjustedLines++;
                    }
                }
                te.ReplaceSelection(string.Join("\n", lines));
                content.text = te.content.text;
                te.selectPos = (startSelectPos - parent.spaceTab.Length > lineStartPos) ? startSelectPos - parent.spaceTab.Length : lineStartPos;
                te.pos = startPos - AdjustedLines * parent.spaceTab.Length;
            }

            private void AdjustSelectPos(TextEditor te)
            {
                if (te.pos < te.selectPos)
                {
                    int _p = te.pos;
                    te.pos = te.selectPos;
                    te.selectPos = _p;
                }
            }

        }

    }

}