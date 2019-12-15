using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "sub",
        ShortDescription: "Make a todo task a child of another task.",
        ErrorText: "",
        LongHelpText: "Tasks exist in a tree. Use this command to move tasks from one parent to another."
    )]
    internal class Sub : ICommand
    {
        [SwitchDocumentation("The ID of the task to move.")]
        [DefaultSwitch(0)] public UInt32 id = 0;
        [DefaultSwitch(1)] public UInt32 parent = 0;

        [SwitchDocumentation("Path to task file.")]
        public string file = "todo.txt";

        public void Invoke(Dictionary<String, Object> PipedArguments)
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

            if (id == parent)
            {
                Console.WriteLine("Not going to work.");
                return;
            }

            var list = EntryList.LoadFile(file, true);

            var entry = list.Root.EnumerateParentChildPairs().FirstOrDefault(e => e.Child.ID == id);
            if (entry.Parent == null || entry.Child == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }

            var newParent = list.Root.FindChildWithID(parent);
            if (newParent == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }

            if (entry.Child.FindChildWithID(parent) != null)
            {
                Console.WriteLine("That would create a circular reference.");
                return;
            }

            entry.Parent.Children.Remove(entry.Child);
            newParent.Children.Add(entry.Child);

            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(newParent, null, 0);
        }
    }
}