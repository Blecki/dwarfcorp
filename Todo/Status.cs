using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "status",
        ShortDescription: "Summarize todo list.",
        ErrorText: "",
        LongHelpText: "Displays task counts in each category.",
        Synonyms: "stat stats"
    )]
    internal class Status : ICommand
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
            var statusHash = new Dictionary<String, int>();
            foreach (var task in list.Root.EnumerateTree())
                if (statusHash.ContainsKey(task.Status))
                    statusHash[task.Status] += 1;
                else
                    statusHash.Add(task.Status, 1);

            var total = statusHash.Values.Sum();

            Presentation.FillBar();
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            foreach (var tag in statusHash)
            {
                Presentation.FillLine();
                Console.WriteLine(String.Format("{0,5} {1} {2:00.00}%", tag.Value, tag.Key, ((float)tag.Value / (float)total * 100.0f)));
            }

            Presentation.FillLine();
            Console.WriteLine(String.Format("{0,5} Total Tasks", total));
            Presentation.FillBar();
            Console.ResetColor();
        }
    }
}