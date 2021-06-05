using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickContent
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = System.IO.File.ReadAllLines(args[0]);

            var foundJsonOrText = false;
            var currentFile = "";
            for (var i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];
                if (line.StartsWith("#begin "))
                {
                    currentFile = line.Substring("#begin ".Length);
                    if (currentFile.EndsWith(".json") || currentFile.EndsWith(".txt") || currentFile.EndsWith(".conv") || currentFile.EndsWith(".font"))
                    {
                        foundJsonOrText = true;
                    }
                }
                else if (line.StartsWith("/build:"))
                {
                    if (foundJsonOrText)
                        lines[i] = "/copy:" + currentFile;
                }
                else if (String.IsNullOrEmpty(line))
                    foundJsonOrText = false;
            }

            System.IO.File.WriteAllLines(args[0], lines);

        }
    }
}
