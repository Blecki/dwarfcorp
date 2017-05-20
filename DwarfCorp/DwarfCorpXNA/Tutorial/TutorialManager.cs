using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Tutorial
{
    public class TutorialManager
    {
        private class TutorialEntry
        {
            public String Text;
            public bool Shown;
        }

        private Dictionary<String, TutorialEntry> Entries;

        private bool TutorialEnabled = true;

        public TutorialManager(String TutorialFile)
        {
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTutorialSet>(
                System.IO.File.ReadAllText(TutorialFile));

            Entries = new Dictionary<string, TutorialEntry>();
            foreach (var entry in entries.Tutorials)
                Entries.Add(entry.Name, new TutorialEntry { Text = entry.Text, Shown = false });
        }

        public TutorialSaveData GetSaveData()
        {
            var r = new TutorialSaveData();
            r.TutorialEnabled = this.TutorialEnabled;
            r.EntryShown = new Dictionary<string, bool>();
            foreach (var entry in Entries)
                r.EntryShown.Add(entry.Key, entry.Value.Shown);
            return r;
        }

        public void SetFromSaveData(TutorialSaveData Data)
        {
            this.TutorialEnabled = Data.TutorialEnabled;
            foreach (var entry in Data.EntryShown)
                if (Entries.ContainsKey(entry.Key)) Entries[entry.Key].Shown = entry.Value;
        }
    }
}
