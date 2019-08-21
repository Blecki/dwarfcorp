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
        [SwitchDocumentation("Path to task file.")]
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
            Presentation.DisplayPaginated(completeList);
        }
    }
}