using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "top",
        ShortDescription: "Shorthand for 'list -p 1'",
        ErrorText: "",
        LongHelpText: ""
    )]
    internal class Top : ICommand
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

            var matcher = Presentation.ComposeMatchers(new StatusMatcher { Status = "-" }, new PriorityMatcher { Priority = 1 });

            var completeList = Presentation.SearchEntries(list.Root, matcher, 0).Where(l => l.Depth >= 0).ToList();
            Presentation.DisplayPaginated(completeList);
        }
    }
}