using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    [Command(
        Name: "next",
        ShortDescription: "",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class Next : ICommand
    {
        public string file = "todo.txt";
        public bool all = false;

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            if (!System.IO.File.Exists(file))
                System.IO.File.WriteAllText(file, "");

            var list = Todo.ParseFile(file).Where(e => all ? true : e.Status == "NEW").ToList();
            
            if (list.Count == 0)
            {
                Console.WriteLine("Todo list is empty.");
                return;
            }

            list.Sort((a, b) => (int)a.ID - (int)b.ID);
            list.Sort((a, b) => (int)b.Priority - (int)a.Priority);

            Todo.OutputEntry(list.First());

        }
    }
}