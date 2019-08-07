using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tools
{
    [Command(
        Name: "del",
        ShortDescription: "",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class Delete : ICommand
    {
        [DefaultSwitch(0)] public UInt32 id = 0;

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

            list.RemoveAll(e => e.ID == id);
            Todo.SaveFile(file, list);
            Console.WriteLine("Deleted {0}.", id);
        }
    }
}