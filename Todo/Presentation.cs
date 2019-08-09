using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.IO;

namespace TodoList
{
    public static class Presentation
    {
        private struct OutputLine
        {
            public int Depth;
            public Entry Entry;
            public Tuple<ConsoleColor, ConsoleColor> Color;
        }

        private static Tuple<ConsoleColor, ConsoleColor> GetStandardColors(Entry Entry)
        {
            if (Entry.Status == "X")
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.Red);
            else if (Entry.Status == "✓")
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.Cyan);
            else
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.DarkGreen);
        }

        private static Tuple<ConsoleColor, ConsoleColor> GetSearchColors(Entry Entry, bool Matched)
        {
            if (Matched)
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.Yellow);
            else
                return GetStandardColors(Entry);
        }

        private static void PrintEntry(OutputLine Line)
        {
            if (Line.Depth < 0) return;

            Console.BackgroundColor = Line.Color.Item1;
            Console.ForegroundColor = Line.Color.Item2;
            var consolePos = Console.CursorTop;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, consolePos);
            for (var i = 0; i < Line.Depth; ++i)
                Console.Write(" |");

            var lineSpace = Console.WindowWidth - (Line.Depth * 2);
            var entryString = Line.Entry.ToString();
            if (entryString.Length >= lineSpace)
            {
                entryString = entryString.Substring(0, lineSpace - 4);
                entryString += "...";
            }

            Console.WriteLine(entryString);
            Console.ResetColor();
        }

        private static List<OutputLine> BuildOutput(Entry Entry, Regex Matcher, int Depth)
        {
            var parentMatch = Matcher == null || Matcher.IsMatch(Entry.Description);
            var r = new List<OutputLine>();
            foreach (var child in Entry.Children)
                r.AddRange(BuildOutput(child, Matcher, Depth + 1));
            if (parentMatch || r.Count > 0)
                r.Insert(0, new OutputLine
                {
                    Depth = Depth,
                    Entry = Entry,
                    Color = Matcher == null ? GetStandardColors(Entry) : GetSearchColors(Entry, parentMatch)
                });
            return r;
        }

        public static void OutputEntry(Entry Entry, Regex Matcher, int Depth)
        {
            foreach (var line in BuildOutput(Entry, Matcher, Depth))
                PrintEntry(line);
        }

        public static void OutputChain(List<Entry> Chain)
        {
            var depth = 0;
            foreach (var item in Chain)
            {
                PrintEntry(new OutputLine { Depth = depth, Entry = item, Color = GetStandardColors(item) });
                depth += 1;
            }
        }
    }
}