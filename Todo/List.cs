using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
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

            var regexMatch = String.IsNullOrEmpty(search) ? null : new RegexMatcher { Pattern = new Regex(search) };
            var tagMatch = String.IsNullOrEmpty(tag) ? null : new TagMatcher { Tag = tag };

            Matcher matcher = new MatchAllMatcher();
            if (regexMatch != null && tagMatch != null)
                matcher = new CompoundMatcher { A = tagMatch, B = regexMatch };
            else if (regexMatch != null)
                matcher = regexMatch;
            else if (tagMatch != null)
                matcher = tagMatch;

            var completeList = Presentation.BuildOutput(entry, matcher, 0, all).Where(l => l.Depth >= 0 && l.Entry.Priority >= p).ToList();
            Presentation.DisplayPaginated(completeList);
        }
    }
}