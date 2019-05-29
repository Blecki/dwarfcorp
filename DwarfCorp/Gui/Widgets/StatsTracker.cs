using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class StatsTracker
    {
        public class Stat
        {
            private const int MaxStats = 1024;

            public struct Entry
            {
                public DateTime Date;
                public float Value;
            }
            public List<Entry> Values = new List<Entry>();
            public void Add(DateTime now, float value)
            {
                if (Values.Count > 0 && (now - Values.Last().Date) < new TimeSpan(0, 1, 0, 0, 0))
                {
                    return;
                }
                Values.Add(new Entry()
                {
                    Date = now,
                    Value = value
                });

                if (Values.Count > MaxStats)
                {
                    Values.RemoveAt(0);
                }
            }
        }

        public Dictionary<string, Stat> GameStats = new Dictionary<string, Stat>();

        public void AddStat(string name, DateTime time, float value)
        {
            if (!GameStats.ContainsKey(name))
            {
                GameStats.Add(name, new Stat());
            }
            GameStats[name].Add(time, value);
        }

    }
}
