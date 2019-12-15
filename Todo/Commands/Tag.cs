using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "tag",
        ShortDescription: "Manage tags.",
        ErrorText: "",
        LongHelpText: "Sets tags on a task. Pass -r to remove the tag instead. Pass -l to list all existing unique tags."
    )]
    internal class Tag : ICommand
    {
        [SwitchDocumentation("The ID of the task to tag.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1)] public String tag = "";
        public bool r = false;
        public bool l = false;

        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke(Dictionary<String, Object> PipedArguments)
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            if (l)
            {
                var list = EntryList.LoadFile(file, false);
                var tagHash = new Dictionary<String, int>();
                foreach (var task in list.Root.EnumerateTree())
                    foreach (var tag in task.Tags)
                        if (tagHash.ContainsKey(tag))
                            tagHash[tag] += 1;
                        else
                            tagHash.Add(tag, 1);

                Presentation.FillBar();
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                foreach (var tag in tagHash)
                {
                    Presentation.FillLine();
                    Console.WriteLine(String.Format("{0,5} {1}", tag.Value, tag.Key));
                }
                Presentation.FillBar();
                Console.ResetColor();
            }
            else
            {
                if (id == 0)
                {
                    Console.WriteLine("You need to specify the entry you're editing.");
                    return;
                }

                if (String.IsNullOrEmpty(tag))
                {
                    Console.WriteLine("You need to specify what tag you are adding or removing.");
                    return;
                }

                var list = EntryList.LoadFile(file, true);
                var entry = list.Root.FindChildWithID(id);

                if (entry == null)
                {
                    Console.WriteLine("Could not find entry with ID{0}.", id);
                    return;
                }

                if (r)
                    entry.Tags.RemoveAll(t => t == tag);
                else if (!entry.Tags.Any(t => t == tag))
                    entry.Tags.Add(tag);

                EntryList.SaveFile(file, list);
                Presentation.OutputEntry(entry, null, 0);
            }
        }
    }
}