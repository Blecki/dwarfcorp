using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "mod",
        ShortDescription: "Change the description of a todo task.",
        ErrorText: "",
        LongHelpText: "Replaces the description of a todo task with the new description provided."
    )]
    internal class Modify : ICommand
    {
        [SwitchDocumentation("The ID of the task to modify.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1), GreedyArgument] public String desc = null;

        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke()
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
            
            if (String.IsNullOrEmpty(desc))
                throw new InvalidOperationException("You need to specify what you're changing it to dumbass.");

            entry.Description = desc;
            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0, false);
        }
    }
}