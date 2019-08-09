using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
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

            var list = EntryList.LoadFile(file, false);

            var iter = all ? list.Root.EnumerateTree() : list.Root.EnumerateTree().Where(e => e.Status == "-");

            var maxPriority = iter.Max(e => e.Priority);          
            var priorityList = iter.Where(e => e.Priority == maxPriority).ToList();
            var minID = priorityList.Min(e => e.ID);
            var parentChain = list.Root.FindParentChain(minID);

            Presentation.OutputChain(parentChain);

            // Todo: Skip when parent is complete / abandoned?
        }
    }
}