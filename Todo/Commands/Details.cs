using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "det",
        ShortDescription: "Show details of a todo task.",
        ErrorText: "",
        LongHelpText: "Not all information about a todo task is displayed by list. Use this command to see everything."
    )]
    internal class Details : ICommand
    {
        [SwitchDocumentation("The ID of the task to display.")]
        [DefaultSwitch(0)] public UInt32 id = 0;

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

            var list = EntryList.LoadFile(file, false);

            var entry = list.Root.FindChildWithID(id);

            if (entry == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }

            Presentation.OutputEntryDetails(entry);
        }
    }
}