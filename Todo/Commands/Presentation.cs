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
            public bool Hilite;
        }

        private static void SetStandardColors(OutputLine Line)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            if (Line.Entry.Status == "X")
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (Line.Entry.Status == "✓")
                Console.ForegroundColor = ConsoleColor.Cyan;
            else if (Line.Entry.Status == "█")
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.DarkGreen;

            if (!Line.Hilite)
                Console.ForegroundColor = ConsoleColor.DarkGray;
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
            SetStandardColors(Line);
            FillLine();
            for (var i = 0; i < Line.Depth; ++i)
                Console.Write(" |");
            Console.WriteLine(FormatEntry(Line.Entry, Console.WindowWidth - (Line.Depth * 2)));
            Console.ResetColor();
        }

        public static List<OutputLine> SearchEntries(Entry Entry, Matcher Matcher, int Depth)
        {
            var parentMatch = Matcher == null || Matcher.Matches(Entry);
            var r = new List<OutputLine>();
            foreach (var child in Entry.Children)
                r.AddRange(SearchEntries(child, Matcher, Depth + 1));
            if (parentMatch || r.Count > 0)
                r.Insert(0, new OutputLine
                {
                    Depth = Depth,
                    Entry = Entry,
                    Hilite = parentMatch,
                });
            return r;
        }

        public static void OutputEntry(Entry Entry, Matcher Matcher, int Depth)
        {
            foreach (var line in SearchEntries(Entry, Matcher, Depth))
                PrintEntry(line);
        }

        public static void OutputChain(List<Entry> Chain)
        {
            var depth = 0;
            foreach (var item in Chain)
            {
                PrintEntry(new OutputLine { Depth = depth, Entry = item, Hilite = false });
                depth += 1;
            }
        }

        public static void OutputEntryDetails(Entry Entry)
        {
            Presentation.FillBar();
            Presentation.OutputEntry(Entry, new MatchAllMatcher(), 0);
            Presentation.FillLine();
            Console.WriteLine(String.Format("Created {0}", Entry.CreationTime));
            Presentation.FillLine();
            Console.WriteLine(Entry.Description);

            if (Entry.Tags.Count > 0)
            {
                Presentation.FillLine();
                Console.WriteLine("Tags: " + String.Join(" ", Entry.Tags));
            }

            if (Entry.Preregs.Count > 0)
            {
                Presentation.FillLine();
                Console.WriteLine("Preregs: " + String.Join(" ", Entry.Preregs.Select(p => String.Format("{0:X4}", p))));
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

        public static Matcher ComposeMatchers(Matcher A, Matcher B)
        {
            if (A == null) return B;
            if (B == null) return A;
            return new AndMatcher { A = A, B = B };
        }
    }
}