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
        public struct OutputLine
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
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(new string(' ', Console.WindowWidth - 1));
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

        public static void PrintEntry(OutputLine Line)
        {
            Console.BackgroundColor = Line.Color.Item1;
            Console.ForegroundColor = Line.Color.Item2;
            FillLine();
            for (var i = 0; i < Line.Depth; ++i)
                Console.Write(" |");
            Console.WriteLine(FormatEntry(Line.Entry, Console.WindowWidth - (Line.Depth * 2)));
            Console.ResetColor();
        }

        public static List<OutputLine> BuildOutput(Entry Entry, Matcher Matcher, int Depth, bool all)
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

        public static void OutputEntryDetails(Entry Entry)
        {
            Presentation.FillBar();
            Presentation.OutputEntry(Entry, null, 0, true);
            Presentation.FillLine();
            Console.WriteLine(String.Format("Created {0}", Entry.CreationTime));
            Presentation.FillLine();
            Console.WriteLine(Entry.Description);

            if (Entry.Tags.Count > 0)
            {
                Presentation.FillLine();
                Console.WriteLine("Tags: " + String.Join(" ", Entry.Tags));
            }

            if (!String.IsNullOrEmpty(Entry.Notes))
            {
                Presentation.FillLine();
                Console.WriteLine("Notes:");
                Presentation.FillLine();
                Console.WriteLine(Entry.Notes);
            }

            Presentation.FillBar();
        }

        public static void DisplayPaginated(List<Presentation.OutputLine> completeList)
        {
            var screenHeight = Console.WindowHeight - 4;

            for (var row = 0; row < completeList.Count; row += screenHeight)
            {
                DisplayPage(completeList, row, screenHeight);
                if (row + screenHeight < completeList.Count)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("q to quit, anything else for more.");
                    var key = Console.ReadKey();
                    if (key.KeyChar == 'q')
                    {
                        Console.ResetColor();
                        return;
                    }
                }
            }


            Console.ResetColor();
        }

        private static void DisplayPage(List<Presentation.OutputLine> Lines, int Start, int Count)
        {
            Presentation.FillBar();
            int shown = 0;
            for (var i = 0; i < Count && i + Start < Lines.Count; ++i)
            {
                shown += 1;
                Presentation.PrintEntry(Lines[i + Start]);
            }
            var pos = Console.CursorTop;
            Presentation.FillBar();
            Console.ForegroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0, pos);
            Console.WriteLine("{0} to {1} of {2}", Start, Start + shown - 1, Lines.Count);
        }
    }
}