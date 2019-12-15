using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "note",
        ShortDescription: "Manage notes.",
        ErrorText: "",
        LongHelpText: "Adds notes to a task. Pass -r to remove all notes instead."
    )]
    internal class Note : ICommand
    {
        [SwitchDocumentation("The ID of the task to anotate.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1), GreedyArgument] public String note = "";
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

            if (id == 0)
            {
                Console.WriteLine("You need to specify the entry you're editing.");
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
                entry.Notes = "";
            else
            {
                if (String.IsNullOrEmpty(note))
                {
                    Console.WriteLine("You need to supply a note.");
                    return;
                }

                if (!String.IsNullOrEmpty(entry.Notes))
                    entry.Notes += "\n";
                entry.Notes += note;
            }

            EntryList.SaveFile(file, list);
            Presentation.OutputEntryDetails(entry);
        }
    }
}