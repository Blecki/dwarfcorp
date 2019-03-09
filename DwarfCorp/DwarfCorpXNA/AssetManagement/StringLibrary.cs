// StringLibrary.cs
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
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class StringLibrary
    {
        private static Dictionary<String, String> Strings = null;

        private static void Initialize()
        {
            Strings = new Dictionary<String, String>();

            var data = FileUtils.LoadConfigurationLinesFromMultipleSources(ContentPaths.Strings);

            foreach (var line in data)
            {
                if (line.Length == 0 || line[0] == ';') continue;
                var colon = line.IndexOf(':');
                if (colon == -1) continue;
                Strings[line.Substring(0, colon)] = line.Substring(colon + 1).Trim();
            }

            if (!Strings.ContainsKey("undefined-string"))
                Strings.Add("undefined-string", "The requested string does not exist in the string library.");
        }

        public static String GetString(String Name)
        {
            if (Strings == null) Initialize();
            String r = null;
            if (Strings.TryGetValue(Name, out r))
                return r;
            else
            {
                return Strings["undefined-string"]; // Oh the irony.
            }
        }

        public static String GetString(String Name, params object[] Arguments)
        {
            try
            {
                return String.Format(GetString(Name), Arguments);
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("String in string library was not well formatted: " + Name);
                return GetString(Name);
            }
        }

        public static String TransformDataString(String Input, String IfEmpty)
        {
            if (String.IsNullOrEmpty(Input)) return IfEmpty;
            if (Input[0] != '@') return Input;
            return GetString(Input.Substring(1));
        }

        [ConsoleCommandHandler("STRINGS")]
        private static String DumpStrings(String arg)
        {
            global::System.IO.File.WriteAllLines("strings.txt", Strings.Select((KeyValuePair<string, string> s) => s.Key + ": " + s.Value));
            return "Dumped strings.";
        }
    }
}