/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace IronPythonConsole
{
    public partial class IPyWindow : EditorWindow
    {
        // Actual class definitions in their own files.
        private partial class ScriptWindow {}
        private partial class HistoryWindow {}
        private static partial class IronPythonInterpreter {}


        [MenuItem("Window/IronPythonConsole")]
        public static void DoIt()
        {
            IPyWindow window = (IPyWindow)EditorWindow.GetWindow(typeof(IPyWindow));
            window.Show();
        }

        // TODO: Setup a custom inpsector window for these settings.
        public GUISkin skin { get; private set; }
        public string spaceTab = "    ";

        // initialization logic (it's Unity, so we don't do this in the constructor!
        public void OnEnable()
        {
            skin = (GUISkin)Resources.Load("EdSkin", typeof(GUISkin));
            script = new ScriptWindow(this);
            history = new HistoryWindow(this);

            // pure gui stuff
            boxBackground = skin.box.normal.background;
            boxBackground.SetPixels(0, 0, 8, 8, new List<Color>(from x in Enumerable.Range(0, 64)
                                                                select Color.black).ToArray());
            boxBackground.Apply();
            consoleBackground = skin.window.normal.background;
            Color bg = new Color(39f / 255f, 40f / 255f, 34f / 255f);
            consoleBackground.SetPixels(0, 0, 8, 8, new List<Color>(from x in Enumerable.Range(0, 64)
                                                                    select bg).ToArray());
            consoleBackground.Apply();

            UpdateLayout();
        }

        public void OnGUI()
        {
            GUI.skin = skin;
            BeginWindows();
            history.Update();
            script.Update();
            EndWindows();
            GUI.Box(spacer, "");
            UpdateLayout();
            ResizeWindows(Event.current);
        }

        private bool _drag = false;
        private Texture2D boxBackground;
        private Texture2D consoleBackground;
        private HistoryWindow history;
        private ScriptWindow script;
        private Rect spacer = new Rect(0f, 192f, 200f, 8f);

        private void UpdateLayout()
        {
            script.position.x = history.position.x = spacer.x = 0f;
            script.position.width = history.position.width = spacer.width = position.width;
            history.position.y = 0;
            history.position.height = spacer.y;
            script.position.y = spacer.y + spacer.height;
            script.position.height = position.height - script.position.y;
        }

        private void ResizeWindows(Event current)
        {
            if (current.type == EventType.mouseDown &&
                spacer.Contains(current.mousePosition))
            {
                _drag = true;
                current.Use();
            }
            if (current.type == EventType.mouseUp && _drag)
            {
                _drag = false;
                current.Use();
            }
            if (current.type == EventType.mouseDrag && _drag)
            {
                // Clamping the value so you can't drag the spacer off screen.
                float y = Event.current.mousePosition.y;
                float min = spacer.height * 2;
                float max = position.height - min;
                spacer.y = (y < min) ? min : (y > max) ? max : y;
                UpdateLayout();
                current.Use();
            }
        }

        // Take the code from the script window.
        // Run it through the interpreter.
        // Push the results to the History window.
        private void Interpret()
        {
            if (!EditorApplication.isPlaying)
            {
                history.text = IronPythonInterpreter.InterpretInMain(script.text);
            }
        }

        // Eventually I want to pass exceptions back to the History Panel.
        // Currently they go to the Unity console, and its annoying have to check two places.
        private void PostError()
        { 

        }


    }

}