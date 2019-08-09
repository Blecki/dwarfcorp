using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "add", // Todo: Support synonyms.
        ShortDescription: "Add a new todo task.",
        ErrorText: "",
        LongHelpText: "Add a new todo task, with the passed description. The description is not optional. The new task will be a child of the root."
    )]
    internal class Add : ICommand
    {
        [DefaultSwitch(0), GreedyArgument] public String desc = null;

        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, true);
            
            if (String.IsNullOrEmpty(desc))
                throw new InvalidOperationException("You need to specify what you're adding dumbass.");

            var entry = new Entry
            {
                ID = list.NextID,
                Status = "-",
                Priority = 0,
                Description = desc
            };

            list.Root.Children.Add(entry);
            list.NextID += 1;

            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0, false);
        }
    }
}