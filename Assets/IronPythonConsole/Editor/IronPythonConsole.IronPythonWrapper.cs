/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEditor;
using IronPython;
using IronPython.Modules;
using IronPython.Hosting;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Scripting.Hosting;
using System.Linq;
using System.IO;

namespace IronPythonConsole
{
    public partial class IPyWindow : EditorWindow
    {

        // The Interpreter is responsible for running the IronPython Scripting Engine.
        // Also it handles the actual running of the scripts.
        // Once upon a time having this up and running for a full session was pretty crash prone.
        // Currently it seems stable, but I don't know if isn't a trick or not.
        [InitializeOnLoad]
        private static partial class IronPythonInterpreter
        {
            

            // Was using this for handling the Pygments Colorizer.
            // But really just keeping it around now for historical purposes.
            // Also there might be a reason to isolate code again?
            public static string InterpretInNewScope(string textToInterpret)
            {
                return Interpret(textToInterpret, CreateScope());
            }

            public static string InterpretInMain(string textToInterpret)
            {
                return Interpret(textToInterpret, main);
            }
            
            public static string Interpret(string textToInterpret, ScriptScope scope)
            {
                string output = "";
                object result = null;
                try
                {
                    ScriptSource source = engine.CreateScriptSourceFromString(textToInterpret);
                    result = source.Execute(scope);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    // Get and reset the screen results.
                    var _buffer = scope.GetVariable("__print_buffer");
                    var gv = engine.Operations.GetMember(_buffer, "getvalue");
                    var st = engine.Operations.Invoke(gv);
                    ScriptSource src = engine.CreateScriptSourceFromString("__print_buffer = sys.stdout = cStringIO.StringIO()");
                    src.Execute(scope);
                    if (st.ToString().Length > 0)
                    {
                        output += "";
                        foreach (string line in st.ToString().Split('\n'))
                            output += "  " + line + "\n";
                        output += "\n";
                    }
                    if (result != null)
                        output += "#  " + result.ToString() + "\n";
                }
                return output;
            }

            private static ScriptEngine engine = Python.CreateEngine();
            private static ScriptScope main;

            static IronPythonInterpreter()
            {
                // Creating a script scope during editor.isPlaying used to be really dangerous.
                // So I'm leaving it off. Also it keeps me from being tempted to use IronPython at runtime.
                if (!EditorApplication.isPlaying)
                {
                    main = CreateScope();
                }
            }

            private static ScriptScope CreateScope()
            {

                StringBuilder startup = new StringBuilder();
                string path = Path.GetDirectoryName(
                            typeof(ScriptEngine).Assembly.Location).Replace(
                            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                List<string> paths = (List<string>)engine.GetSearchPaths();
                paths.Add(Path.Combine(path, "Lib"));
                paths.Add(Path.Combine(path, "DLLs"));
                paths.Add(Path.Combine(path, "Lib/site-packages"));
                engine.SetSearchPaths(paths);

                // Populating the main namespace with some basic modules.
                startup.AppendLine("import sys");
                startup.AppendLine("import UnityEngine as unity");
                startup.AppendLine("import UnityEditor as editor");
                startup.AppendLine("import cStringIO");
                startup.AppendLine("log = unity.Debug.Log");
                startup.AppendLine("__print_buffer = sys.stdout = cStringIO.StringIO()");
                
                // Making sure it actually starts up.
                startup.AppendLine("log('IronPython is a go.')");

                ScriptScope scope = engine.CreateScope();
                engine.Runtime.LoadAssembly(typeof(PythonIOModule).Assembly);
                engine.Runtime.LoadAssembly(typeof(GameObject).Assembly);
                engine.Runtime.LoadAssembly(typeof(Editor).Assembly);
                ScriptSource Source = engine.CreateScriptSourceFromString(startup.ToString());
                Source.Execute(scope);
                return scope;
            }

 
        }

    }

}
