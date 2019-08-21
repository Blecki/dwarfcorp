using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "del",
        ShortDescription: "Deletes a todo task.",
        ErrorText: "",
        LongHelpText: "Deletes a todo task. Will fail if the task has children; to delete an task with children, pass the -r flag. In such a case, all children are also deleted."
    )]
    internal class Delete : ICommand
    {
        [SwitchDocumentation("The ID of the task to delete.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        public bool r = false;

        [SwitchDocumentation("Path to task file.")]
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

            var list = EntryList.LoadFile(file, true);
            var toDelete = list.Root.EnumerateParentChildPairs().FirstOrDefault(e => e.Child.ID == id);
            
            if (toDelete.Parent == null || toDelete.Child == null)
            {
                Console.WriteLine("No entry with id {0} found.", id);
                return;
            }

            if (toDelete.Child.Children.Count > 0 && !r)
            {
                Console.WriteLine("Entry {0} has children. To delete anyway, pass -r", id);
                return;
            }

            toDelete.Parent.Children.Remove(toDelete.Child);
            EntryList.SaveFile(file, list);
            Console.WriteLine("Deleted {0:X4}.", id);
        }
    }
}