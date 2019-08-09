using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "list",
        ShortDescription: "Lists todo tasks.",
        ErrorText: "",
        LongHelpText: "Prints a list of todo tasks. Pass an id with -id to list a specific task and its children. Pass a regex pattern with -search to list only tasks (and their parents) that match the pattern. By default, skips tasks marked complete or abandoned. Pass -all to list all tasks regardless of status."
    )]
    internal class List : ICommand
    {
        public UInt32 id = 0;
        [GreedyArgument] public String search = "";
        public bool all = false;
        

        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, false);

            var entry = list.Root.FindChildWithID(id);
            if (entry == null)
            {
                Console.WriteLine("No entry with id {0} found.", id);
                return;
            }

            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(new string(' ', Console.WindowWidth));

            if (String.IsNullOrEmpty(search))
                Presentation.OutputEntry(entry, null, -1, all);
            else
                Presentation.OutputEntry(entry, new Regex(search), -1, all);

            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.ResetColor();
        }
    }
}