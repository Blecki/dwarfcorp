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
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.DarkRed);
            else if (Entry.Status == "✓")
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.Cyan);
            else if (Entry.Status == "█")
                return Tuple.Create(ConsoleColor.Black, ConsoleColor.Red);
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

        public static void FillLine()
        {
            var consolePos = Console.CursorTop;
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, consolePos);
        }

        public static void FillBar()
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(new string(' ', Console.WindowWidth));
        }

        public static String Fit(String S, int Length)
        {
            if (Length <= 2) return "";
            if (S.Length > Length)
                return S.Substring(0, Length - 3) + "..";
            else
                return S;
        }

        public static String FormatEntry(Entry Entry, int Length)
        {
            var r = String.Format("{0:X4} {1,1} {2:X2} ", Entry.ID, Entry.Status, Entry.Priority);
            var tags = String.Join(" ", Entry.Tags);
            if (tags.Length > 0) r += "[" + Fit(tags, 20) + "] ";
            r += Fit(Entry.Description, Length - r.Length);
            return r;
        }

        private static void PrintEntry(OutputLine Line)
        {
            if (Line.Depth < 0) return;

            Console.BackgroundColor = Line.Color.Item1;
            Console.ForegroundColor = Line.Color.Item2;
            FillLine();
            for (var i = 0; i < Line.Depth; ++i)
                Console.Write(" |");
            Console.WriteLine(FormatEntry(Line.Entry, Console.WindowWidth - (Line.Depth * 2)));
            Console.ResetColor();
        }

        private static List<OutputLine> BuildOutput(Entry Entry, Matcher Matcher, int Depth, bool all)
        {
            var parentMatch = (all || Entry.Status == "-") && (Matcher == null || Matcher.Matches(Entry));
            var r = new List<OutputLine>();
            foreach (var child in Entry.Children)
                r.AddRange(BuildOutput(child, Matcher, Depth + 1, all));
            if (parentMatch || r.Count > 0)
                r.Insert(0, new OutputLine
                {
                    Depth = Depth,
                    Entry = Entry,
                    Color = (Matcher != null && Matcher.Hilite) ? GetSearchColors(Entry, parentMatch) : GetStandardColors(Entry)
                });
            return r;
        }

        public static void OutputEntry(Entry Entry, Matcher Matcher, int Depth, bool all)
        {
            foreach (var line in BuildOutput(Entry, Matcher, Depth, all))
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