using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "pre",
        Synonyms: "first",
        ShortDescription: "Manage prerequisites.",
        ErrorText: "",
        LongHelpText: "Sets a prerequisite on a task. Pass -r to remove the prereg instead."
    )]
    internal class Prereg : ICommand
    {
        [SwitchDocumentation("The ID of the task to modify.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1)] public UInt32 pre = 0;
        public bool r = false;

        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke(Dictionary<String, Object> PipedArguments)
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            {
                if (id == 0)
                {
                    Console.WriteLine("You need to specify the entry you're editing.");
                    return;
                }

                if (pre == 0)
                {
                    Console.WriteLine("You need to specify the prereg.");
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
                    entry.Preregs.RemoveAll(t => t == pre);
                else if (!entry.Preregs.Any(t => t == pre))
                    entry.Preregs.Add(pre);

                EntryList.SaveFile(file, list);
                Presentation.OutputEntry(entry, null, 0);
            }
        }
    }
}