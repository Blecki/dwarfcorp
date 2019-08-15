using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "pull-update",
        ShortDescription: "Lists todo tasks.",
        ErrorText: "",
        LongHelpText: "Prints a list of todo tasks. Pass an id with -id to list a specific task and its children. Pass a regex pattern with -search to list only tasks (and their parents) that match the pattern. By default, skips tasks marked complete or abandoned. Pass -all to list all tasks regardless of status."
    )]
    internal class PullUpdate : ICommand
    {
        [DefaultSwitch(0)] public UInt32 days = 7;
        public string file = "todo.txt";

        private class TimespanMatcher : Matcher
        {
            public bool Hilite => false;
            public UInt32 Days;

            public bool Matches(Entry Entry)
            {
                return Entry.Status == "✓"
                    && Entry.CompletionTime > (DateTime.Now - new TimeSpan((int)Days, 0, 0, 0));
            }
        }

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, false);

            var completeList = Presentation.BuildOutput(list.Root, new TimespanMatcher { Days = days }, -1, true).Where(l => l.Depth >= 0).ToList();
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

        private void DisplayPage(List<Presentation.OutputLine> Lines, int Start, int Count)
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