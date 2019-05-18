using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Newtonsoft.Json;
#if !XNA_BUILD && !GEMMONO
using SDL2;
#endif
using SharpRaven;
using SharpRaven.Data;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class EventLog
    {
        public struct LogEntry
        {
            public string Text;
            public string Details;
            public DateTime Date;
            public Color TextColor;
        }

        private List<LogEntry> Entries = new List<LogEntry>();
        private TimeSpan MaxDuration = new TimeSpan(10, 0, 0, 0, 0);

        public IEnumerable<LogEntry> GetEntries()
        {
            return Entries;
        }

        public void AddEntry(LogEntry entry)
        {
            // Deduplication of entries.
            if (Entries.Any(e => e.Text == entry.Text && (entry.Date - e.Date) < new TimeSpan(0, 1, 0, 0, 0)))
                return;

            Console.Out.WriteLine(entry.Text);
            Entries.Add(entry);
            Entries.RemoveAll(e => (entry.Date - e.Date) > MaxDuration);
        }

        public EventLog()
        {

        }
    }
}
