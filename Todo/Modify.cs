using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    [Command(
        Name: "mod",
        ShortDescription: "",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class Modify : ICommand
    {
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1), GreedyArgument] public String argument = null;

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

            if (!System.IO.File.Exists(file))
                System.IO.File.WriteAllText(file, "");

            var list = Todo.ParseFile(file);

            var entry = list.FirstOrDefault(e => e.ID == id);
            if (entry == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }
            
            if (String.IsNullOrEmpty(argument))
                throw new InvalidOperationException("You need to specify what you're changing it to dumbass.");

            entry.Description = argument;
            Todo.SaveFile(file, list);
            Todo.OutputEntry(entry);
        }
    }
}