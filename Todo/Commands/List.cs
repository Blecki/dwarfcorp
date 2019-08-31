using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    public enum SortOrder
    {
        DEFAULT,
        COMPLETION,
    }

    [Command(
        Name: "list",
        ShortDescription: "Lists todo tasks.",
        ErrorText: "",
        LongHelpText: "Prints a list of todo tasks. Pass an id with -id to list a specific task and its children. Pass a regex pattern with -search to list only tasks (and their parents) that match the pattern. By default, skips tasks marked complete or abandoned. Pass -all to list all tasks regardless of status."
    )]
    internal class List : ICommand
    {
        public UInt32 id = 0;
        [DefaultSwitch(0), GreedyArgument] public String search = "";
        public UInt32 p = 0;
        public bool all = false;
        public String tag = "";
        public UInt32 days = 0;
        public UInt32 cdays = 0;
        public SortOrder order = SortOrder.DEFAULT;

        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, false);

            var entry = list.Root.FindChildWithID(id);
            if (entry == null)
            {
                Console.WriteLine("No entry with id {0} found.", id);
                return;
            }

            var matcher = (all || cdays > 0) ? (Matcher)(new MatchAllMatcher()) : new StatusMatcher { Status = "-" };
            if (!String.IsNullOrEmpty(search)) matcher = Presentation.ComposeMatchers(matcher, new RegexMatcher { Pattern = new Regex(search) });
            if (!String.IsNullOrEmpty(tag)) matcher = Presentation.ComposeMatchers(matcher, new TagMatcher { Tag = tag });
            if (p > 0) matcher = Presentation.ComposeMatchers(matcher, new PriorityMatcher { Priority = p });
            if (days > 0) matcher = Presentation.ComposeMatchers(matcher, new CreatedTimespanMatcher { Days = days });
            if (cdays > 0) matcher = Presentation.ComposeMatchers(matcher, new CompletedTimespanMatcher { Days = cdays });

            var completeList = Presentation.SearchEntries(entry, matcher, 0).Where(l => l.Depth >= 0).ToList();

            if (order == SortOrder.COMPLETION)
                completeList.Sort((a, b) => (int)(a.Entry.CompletionTime - b.Entry.CompletionTime).TotalMilliseconds);

            Presentation.DisplayPaginated(completeList);
        }
    }
}