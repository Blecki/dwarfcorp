using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "delay",
        ShortDescription: "Marks a todo task as delayed.",
        ErrorText: "",
        LongHelpText: "Change the status of a todo task to delayed. This will cause next to skip this task. It also makes the task display in orange."
    )]
    internal class Delay : ICommand
    {
        [SwitchDocumentation("The ID of the task to mark delayed.")]
        [DefaultSwitch(0)] public UInt32 id = 0;

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

            entry.Status = "█";
            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0);
        }
    }
}