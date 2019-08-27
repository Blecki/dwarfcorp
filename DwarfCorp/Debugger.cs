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
            public static bool DrawPaths = false;
            public static bool DrawRailNetwork = false;
            public static bool DrawPipeNetwork = false;
            public static bool DrawToolDebugInfo = false;
            public static bool HideTerrain = false;
            public static bool ABTestSwitch = false;
            public static bool DrawComposites = false;
            public static bool DrawSelectionBuffer = false;
            public static bool HideSliceTop = false;
            public static bool DebugElevators = false;
            public static bool DrawUpdateBox = false;
            public static bool DisableWaterUpdate = false;
            public static bool DrawInvisible = false;
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
