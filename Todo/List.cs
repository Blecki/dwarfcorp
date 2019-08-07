using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    [Command(
        Name: "list",
        ShortDescription: "",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class List : ICommand
    {
        [DefaultSwitch(0), GreedyArgument] public String search = "";


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
            
            var count = 0;
            if (String.IsNullOrEmpty(search))
                foreach (var e in list)
                {
                    Todo.OutputEntry(e);
                    count += 1;
                }
            else
            {
                var regex = new Regex(search);
                foreach (var e in list.Where(e => regex.IsMatch(e.Description)))
                {
                    Todo.OutputEntry(e);
                    count += 1;
                }
            }

            Console.WriteLine("{0} entries found", count);
        }
    }
}