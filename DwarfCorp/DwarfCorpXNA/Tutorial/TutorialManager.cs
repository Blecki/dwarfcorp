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
        }

        private Dictionary<String, TutorialEntry> Entries;
        public bool TutorialEnabled = true;
        private String PendingTutorial = null;
        private Widget ExistingTutorial = null;
        private bool TutorialVisible = false;

        public TutorialManager(String TutorialFile)
        {
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTutorialSet>(
                System.IO.File.ReadAllText(TutorialFile));

            Entries = new Dictionary<string, TutorialEntry>();
            foreach (var entry in entries.Tutorials)
                Entries.Add(entry.Name, new TutorialEntry
                {
                    Text = entry.Text,
                    Shown = false,
                    Title = entry.Title,
                    GuiHilite = entry.GuiHilite
                });
        }

        public void ResetTutorials()
        {
            foreach (var entry in Entries)
                entry.Value.Shown = false;
        }

        public void ShowTutorial(String Name)
        {
            // Queue this and show on next frame in an update func.
            if (TutorialEnabled && Entries.ContainsKey(Name) && !Entries[Name].Shown)
                PendingTutorial = Name;
        }

        public void Update(Gui.Root Gui)
        {

            if (!String.IsNullOrEmpty(PendingTutorial) && Gui != null &&!Entries[PendingTutorial].Shown)
            {
                if (TutorialVisible && ExistingTutorial != null)
                {
                    ExistingTutorial.Close();
                }

                var entry = Entries[PendingTutorial];
                entry.Shown = true;
                TutorialVisible = true;

                var popup = Gui.ConstructWidget(new Gui.Widgets.TutorialPopup
                {
                    Message = entry,
                    OnClose = (sender) =>
                    {
                        TutorialEnabled = !(sender as Gui.Widgets.TutorialPopup).DisableChecked;
                        TutorialVisible = false;
                        Gui.ClearSpecials();
                    },
                    OnLayout = (sender) =>
                    {
                        sender.Rect.X = Gui.RenderData.VirtualScreen.Width - sender.Rect.Width;
                        sender.Rect.Y = 64;
                    }
                });
                ExistingTutorial = popup;
                Gui.ShowDialog(popup);
                PendingTutorial = null;

                if (!String.IsNullOrEmpty(entry.GuiHilite))
                {
                    var widget = Gui.RootItem.EnumerateTree().FirstOrDefault(w =>
                        w.Tag is String && (w.Tag as String) == entry.GuiHilite);
                    if (widget != null)
                    {
                        Gui.SpecialHiliteRegion = widget.Rect;
                        Gui.SpecialHiliteSheet = "border-hilite";

                        if (widget.Rect.Right < Gui.RenderData.VirtualScreen.Width / 2 ||
                            widget.Rect.Left < 64)
                        {
                            Gui.SpecialIndicatorPosition = new Microsoft.Xna.Framework.Point(
                                widget.Rect.Right, widget.Rect.Center.Y- 16);
                            Gui.SpecialIndicator = new Gui.MousePointer("hand", 1, 10);
                        }
                        else
                        {
                            Gui.SpecialIndicatorPosition = new Microsoft.Xna.Framework.Point(
                                widget.Rect.Left - 32, widget.Rect.Center.Y - 16);
                            Gui.SpecialIndicator = new Gui.MousePointer("hand", 4, 14);
                        }
                        widget.OnClick += (sender, args) =>
                        {
                            popup.Close();
                        };
                    }
                    else
                    {
                        Console.Error.WriteLine("GUI highlight {0} does not exist.", entry.GuiHilite);
                    }
                }
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
