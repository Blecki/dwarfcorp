using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "set",
        ShortDescription: "Set priority of a todo task.",
        ErrorText: "",
        LongHelpText: "Sets the priority of a todo task. Priority range is [0,FF]. Higher values mean higher priority."
    )]
    internal class Set : ICommand
    {
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1)] public UInt32 priority = 0;

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

            if (priority < 0 || priority > 0xFF)
            {
                Console.WriteLine("Valid range for option priority is [0,FF]");
                return;
            }

            var list = EntryList.LoadFile(file, true);

            var entry = list.Root.FindChildWithID(id);

            if (entry == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }

            entry.Priority = priority;
            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0, false);
        }
    }
}