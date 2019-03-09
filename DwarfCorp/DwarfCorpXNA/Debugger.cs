// Debugger.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
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
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Provides static access to debugger switches toggleable on god menu
    /// </summary>
    public static class Debugger
    {
        public static class Switches
        {
            public static bool DrawBoundingBoxes = false;
            public static bool DrawOcttree = false;
            public static bool DrawPaths = false;
            public static bool DrawRailNetwork = false;
            public static bool DrawPipeNetwork = false;
            public static bool DrawToolDebugInfo = false;
            public static bool HideTerrain = false;
            public static bool ABTestSwitch = false;
            public static bool DrawComposites = false;
            public static bool DrawSelectionBuffer = false;
            public static bool HideSliceTop = false;
        }

        public class Switch
        {
            public String Name;
            public Action<bool> Set;
            public bool State;
        }

        public static IEnumerable<Switch> EnumerateSwitches()
        {
            foreach (var member in typeof(Switches).GetFields(global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static))
            {
                global::System.Diagnostics.Debug.Assert(member.FieldType == typeof(bool));
                yield return new Switch
                {
                    Name = member.Name,
                    Set = (value) => member.SetValue(null, value),
                    State = (member.GetValue(null) as bool?).Value
                };
            }
        }

        internal static string GetNicelyFormattedName(string Name)
        {
            var r = "";
            foreach (var c in Name)
            {
                if (c >= 'A' && c <= 'Z' && !String.IsNullOrEmpty(r))
                    r += " ";
                r += c;
            }
            return r;
        }

        private static Dictionary<String, Func<String, String>> CommandHandlers = null;

        public static String HandleConsoleCommand(String Command)
        {
            if (CommandHandlers == null)
            {
                CommandHandlers = new Dictionary<string, Func<string, string>>();
                foreach (var hook in AssetManager.EnumerateModHooks(typeof(ConsoleCommandHandlerAttribute), typeof(String), new Type[] { typeof(String) }))
                {
                    var lambdaCopy = hook;
                    var attribute = hook.GetCustomAttributes(false).FirstOrDefault(a => a is ConsoleCommandHandlerAttribute) as ConsoleCommandHandlerAttribute;
                    if (attribute == null) continue;
                    CommandHandlers[attribute.Name.ToUpperInvariant()] = (s) => lambdaCopy.Invoke(null, new Object[] { s }) as String;
                }
            }

            var commandWord = "";
            var commandArgs = "";
            var space = Command.IndexOf(' ');
            if (space == -1)
                commandWord = Command;
            else
            {
                commandWord = Command.Substring(0, space);
                commandArgs = Command.Substring(space + 1);
            }

            if (CommandHandlers.ContainsKey(commandWord.ToUpperInvariant()))
                return CommandHandlers[commandWord.ToUpperInvariant()](commandArgs);
            else
                return "Unknown command.";
        }

        [ConsoleCommandHandler("HELP")]
        public static string ListSettings(String Name)
        {
            var builder = new StringBuilder();
            foreach (var hook in AssetManager.EnumerateModHooks(typeof(ConsoleCommandHandlerAttribute), typeof(String), new Type[] { typeof(String) }))
            {
                var lambdaCopy = hook;
                var attribute = hook.GetCustomAttributes(false).FirstOrDefault(a => a is ConsoleCommandHandlerAttribute) as ConsoleCommandHandlerAttribute;
                if (attribute == null) continue;
                builder.AppendLine(attribute.Name.ToUpperInvariant());
            }
            return builder.ToString();
        }
    }
}
