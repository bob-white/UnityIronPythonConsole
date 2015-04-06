/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace IronPythonConsole
{
    public partial class IPyWindow : EditorWindow
    {
        // The history window is nice and simple.
        // It just needs to display output from the interpreter.
        private partial class HistoryWindow
        {
            public Rect position = new Rect();
            public string text
            {
                get
                {
                    return content.text;
                }
                set
                {
                    content.text = value;
                }
            }

            public HistoryWindow(IPyWindow parent)
            {
                content = new GUIContent();
                this.parent = parent;
            }

            public void Update()
            {
                GUI.Window(20, position, Create, "");
            }

            public void Create(int id)
            {
                scrollView = GUILayout.BeginScrollView(scrollView);
                content.text = GUILayout.TextArea(content.text);
                GUILayout.EndScrollView();
            }

            private GUIContent content;
            private Vector2 scrollView = new Vector2();
            private IPyWindow parent;
        }
    }
}