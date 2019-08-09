using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "next",
        ShortDescription: "Identify and show the next best task.",
        ErrorText: "",
        LongHelpText: "Identifies the next best task based on priority and ID. All tasks are first sorted by priority, then by ID, such that this will display the highest priority task with the lowest ID."
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
            // Todo: When a parent is high priority, and has incomplete children, display the child instead.
        }
    }
}