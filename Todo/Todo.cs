using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class Todo
{
    public class Entry
    {
        public UInt32 ID;
        public String Status;
        public UInt32 Priority;
        public String Description;

        public List<Entry> SubEntries = new List<Entry>();
    }

    public static List<Entry> ParseFile(String File)
    {
        var f = System.IO.File.ReadAllLines(File);
        var r = new List<Entry>();

        Entry last = null;

        foreach (var l in f)
        {
            try {
                var e = ParseEntry(l);
                r.Add(e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not parse todo entry.");
                Console.WriteLine(l);
                Console.WriteLine(e.Message);
            }
        }
        return r;
    }

    public static Entry ParseEntry(String Line)
    {
        var firstSpace = Line.IndexOf(' ');
        var secondSpace = Line.IndexOf(' ', firstSpace + 1);
        var thirdSpace = Line.IndexOf(' ', secondSpace + 1);
 
        return new Entry
        {
            ID = UInt32.Parse(Line.Substring(0, firstSpace)),
            Status = Line.Substring(firstSpace + 1, secondSpace - firstSpace - 1),
            Priority = UInt32.Parse(Line.Substring(secondSpace + 1, thirdSpace - secondSpace - 1)),
            Description = Line.Substring(thirdSpace + 1)
        };
    }

    public static String EmitEntry(Entry Entry)
    {
        return Entry.ID.ToString() + " " + Entry.Status + " " + Entry.Priority + " " + Entry.Description;
    }

    public static String EmitEntries(IEnumerable<Entry> Entries)
    {
        var builder = new StringBuilder();
        foreach (var entry in Entries)
            builder.AppendLine(EmitEntry(entry));
        return builder.ToString();
    }

    public static void SaveFile(String File, List<Entry> Entries)
    {
        System.IO.File.WriteAllText(File, EmitEntries(Entries));
    }

    public static void OutputEntry(Entry Entry)
    {
        if (Entry.Status == "ABANDONED")
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Red;
        }
        else if (Entry.Status == "COMPLETE")
        {
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Blue;
        }
        else
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
        }

        Console.WriteLine(EmitEntry(Entry));

        Console.ResetColor();
    }
}