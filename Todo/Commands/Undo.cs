using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "undo",
        ShortDescription: "Undoes the last operation.",
        ErrorText: "",
        LongHelpText: "Undoes the last operation. Only applies to operations that modify tasks or the task tree."
    )]
    internal class Undo : ICommand
    {
        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            var list = EntryList.LoadFile(file, false);
            if (list.PreviousVersions.Count > 0)
            {
                list.Root = list.PreviousVersions[list.PreviousVersions.Count - 1];
                list.PreviousVersions.RemoveAt(list.PreviousVersions.Count - 1);
            }
            else
            {
                Console.WriteLine("Nothing left to undo.");
            }

            EntryList.SaveFile(file, list);
        }
    }
}