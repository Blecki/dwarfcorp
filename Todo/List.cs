using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
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

            var list = EntryList.LoadFile(file, false);
            
            if (String.IsNullOrEmpty(search))
                Presentation.OutputEntry(list.Root, null, -1);
            else
                Presentation.OutputEntry(list.Root, new Regex(search), -1);
        }
    }
}