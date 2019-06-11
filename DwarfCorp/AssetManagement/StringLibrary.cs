using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<String, String> Strings = null;

        private static void InitializeStrings()
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
            if (Strings == null) InitializeStrings();

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