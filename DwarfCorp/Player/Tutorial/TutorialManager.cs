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
        private Widget ExistingTutorial = null;
        private bool TutorialVisible = false;

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
            }
        }

        public void HideTutorial()
        {
            TutorialVisible = false;

            if (ExistingTutorial != null)
                ExistingTutorial.Hidden = true;
        }

        public void ShowTutorial()
        {
            TutorialVisible = true;
            if (ExistingTutorial != null)
                ExistingTutorial.Hidden = false;
        }

        public void Update(Gui.Root Gui)
        {
            if (!TutorialEnabled || Gui == null)
                return;

            if (ExistingTutorial != null) return;

            if (PendingTutorials.Count > 0)
            {
                var nextTutorial = PendingTutorials.Dequeue();
                if (Entries.ContainsKey(nextTutorial))
                {
                    var entry = Entries[nextTutorial];
                    TutorialVisible = true;
                    ExistingTutorial = CreateTutorialPopup(entry, Gui);
                }
            }
        }

        private Widget CreateTutorialPopup(TutorialEntry Tutorial, Root Gui)
        {
            var popup = Gui.ConstructWidget(new Gui.Widgets.TutorialPopup
            {
                Message = Tutorial,
                OnClose = (sender) =>
                {
                    TutorialEnabled = !(sender as Gui.Widgets.TutorialPopup).DisableChecked;
                    Gui.ClearSpecials();
                    if (!String.IsNullOrEmpty(Tutorial.NextTutorial))
                        ShowTutorial(Tutorial.NextTutorial);
                    ExistingTutorial = null;
                },
                OnLayout = (sender) =>
                {
                    sender.Rect.X = Gui.RenderData.VirtualScreen.Width - sender.Rect.Width;
                    sender.Rect.Y = 64;
                }
            });

            if (Tutorial.Popup)
            {
                Gui.ShowMinorPopup(popup);
                popup.PopupDestructionType = PopupDestructionType.Keep;
            }
            else
                Gui.RootItem.AddChild(popup);

            Gui.SpecialHiliteWidgetName = Tutorial.GuiHilite;

            return popup;
        }

        public bool HasCurrentTutorial()
        {
            return ExistingTutorial != null;
        }

        public void DismissCurrentTutorial()
        {
            if (ExistingTutorial != null)
                ExistingTutorial.Close();
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
