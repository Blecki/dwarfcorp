using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;

namespace DwarfCorp.Tutorial
{
    public class TutorialManager
    {
        public class TutorialEntry
        {
            public String Title;
            public String Text;
            public bool Shown;
            public String GuiHilite;
            public bool Popup = false;
            public String Name;
            public ResourceType.GuiGraphic Icon;
            public String NextTutorial;
        }

        private Dictionary<String, TutorialEntry> Entries;
        public bool TutorialEnabled = true;
        private Gui.Widgets.TutorialPopup ExistingTutorial = null;
        public bool FlipTutorial = true;

        private Queue<String> PendingTutorials = new Queue<string>();

        public TutorialManager()
        {
            var entries = FileUtils.LoadJsonListFromMultipleSources<JsonTutorialEntry>(ContentPaths.tutorials, null, t => t.Name);

            Entries = new Dictionary<string, TutorialEntry>();
            foreach (var entry in entries)
                Entries.Add(entry.Name, new TutorialEntry
                {
                    Text = entry.Text,
                    Shown = false,
                    Title = entry.Title,
                    GuiHilite = entry.GuiHilite,
                    Popup = entry.Popup,
                    NextTutorial = entry.NextTutorial
                });
        }

        public void AddTutorial(string name, string text, ResourceType.GuiGraphic Icon = null)
        {
            Entries[name] = new TutorialEntry()
            {
                Text = text,
                Title = name,
                Shown = false,
                Popup = false,
                Icon = Icon
            };
        }

        public Dictionary<String, TutorialEntry> EnumerateTutorials()
        {
            return Entries;
        }

        public void ResetTutorials()
        {
            foreach (var entry in Entries)
                entry.Value.Shown = false;
        }

        public void ShowTutorial(String Name)
        {
            if (TutorialEnabled && Entries.ContainsKey(Name) && !Entries[Name].Shown)
            {
                Entries[Name].Shown = true;
                PendingTutorials.Enqueue(Name);
                FlipTutorial = true;
            }
        }

        public void HideTutorial()
        {
            if (ExistingTutorial != null)
                ExistingTutorial.Hidden = true;
        }

        public void ShowTutorial()
        {
            if (ExistingTutorial != null)
                ExistingTutorial.Hidden = false;
        }

        public void Update(Gui.Root Gui)
        {
            if (!TutorialEnabled && ExistingTutorial != null)
                ExistingTutorial.Hidden = true;

            if (!TutorialEnabled || Gui == null)
                return;

            if (FlipTutorial == true)
            {
                FlipTutorial = false;

                if (PendingTutorials.Count > 0)
                {
                    var nextTutorial = PendingTutorials.Dequeue();
                    if (Entries.ContainsKey(nextTutorial))
                    {
                        var entry = Entries[nextTutorial];
                        CreateTutorialPopup(entry, Gui);
                    }
                    else
                        ExistingTutorial.Hidden = true;
                }
                else
                {
                    if (ExistingTutorial != null)
                        ExistingTutorial.Hidden = true;
                }
            }
        }

        private void DismissTutorial(Root Gui)
        {
            TutorialEnabled = !ExistingTutorial.DisableChecked;
            Gui.ClearSpecials();
            if (!String.IsNullOrEmpty(ExistingTutorial.Message.NextTutorial))
                ShowTutorial(ExistingTutorial.Message.NextTutorial);
            FlipTutorial = true;
        }

        private void CreateTutorialPopup(TutorialEntry Tutorial, Root Gui)
        {
            if (ExistingTutorial == null)
                ExistingTutorial = Gui.RootItem.AddChild(new Gui.Widgets.TutorialPopup
                {
                    Message = Tutorial,
                    Hidden = true,
                    OnDismiss = (sender) => { DismissTutorial(Gui); },
                    StartingSize = new Microsoft.Xna.Framework.Rectangle(Gui.RenderData.VirtualScreen.Width - 400, 64, 400, 450)
                }) as Gui.Widgets.TutorialPopup;

            ExistingTutorial.Hidden = false;
            ExistingTutorial.Message = Tutorial;
            ExistingTutorial.Refresh();
            Gui.SpecialHiliteWidgetName = Tutorial.GuiHilite;
            ExistingTutorial.Invalidate();
        }

        public bool HasCurrentTutorial()
        {
            return ExistingTutorial != null && !ExistingTutorial.Hidden;
        }

        public void DismissCurrentTutorial()
        {
            if (ExistingTutorial != null && !ExistingTutorial.Hidden)
                DismissTutorial(ExistingTutorial.Root);
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
