using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    [Command(
        Name: "add",
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

            if (!System.IO.File.Exists(file))
                System.IO.File.WriteAllText(file, "");

            var list = Todo.ParseFile(file);
            
            if (String.IsNullOrEmpty(argument))
                throw new InvalidOperationException("You need to specify what you're adding dumbass.");

            var highest = list.Count == 0 ? 0 : list.Max(e => e.ID);    
            
            var entry = new Todo.Entry
            {
                ID = highest + 1,
                Status = "NEW",
                Priority = 0,
                Description = argument
            };

            list.Add(entry);

            Todo.SaveFile(file, list);
            Todo.OutputEntry(entry);
        }
    }
}