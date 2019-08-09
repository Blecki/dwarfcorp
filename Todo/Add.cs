using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "add", // Todo: Support synonyms.
        ShortDescription: "",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class Add : ICommand
    {
        [DefaultSwitch(0), GreedyArgument] public String argument = null;

        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, true);
            
            if (String.IsNullOrEmpty(argument))
                throw new InvalidOperationException("You need to specify what you're adding dumbass.");

            var entry = new Entry
            {
                ID = list.NextID,
                Status = "-",
                Priority = 0,
                Description = argument
            };

            list.Root.Children.Add(entry);
            list.NextID += 1;

            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0);
        }
    }
}