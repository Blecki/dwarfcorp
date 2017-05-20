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
        private String PendingTutorial = null;

        public TutorialManager(String TutorialFile)
        {
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTutorialSet>(
                System.IO.File.ReadAllText(TutorialFile));

            Entries = new Dictionary<string, TutorialEntry>();
            foreach (var entry in entries.Tutorials)
                Entries.Add(entry.Name, new TutorialEntry { Text = entry.Text, Shown = false });
        }

        public void ShowTutorial(String Name)
        {
            // Queue this and show on next frame in an update func.
            if (TutorialEnabled && Entries.ContainsKey(Name) && !Entries[Name].Shown)
                PendingTutorial = Name;
        }

        public void Update(Func<String, bool> GuiHook)
        {
            if (!String.IsNullOrEmpty(PendingTutorial) && GuiHook != null)
            { 
                Entries[PendingTutorial].Shown = true;
                TutorialEnabled = !GuiHook(Entries[PendingTutorial].Text);
            }
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
