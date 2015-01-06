using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class AnnounementViewer : GUIComponent
    {

        public class AnnouncementView : Panel
        {
            public Label Label { get; set; }
            public ImagePanel Icon { get; set; }

            public AnnouncementView(DwarfGUI gui, GUIComponent parent) :
                base(gui, parent)
            {
                GridLayout layout = new GridLayout(gui, this, 1, 4);

                Icon = new ImagePanel(GUI, layout, new ImageFrame())
                {
                    ConstrainSize = true
                };

                layout.SetComponentPosition(Icon, 0, 0, 1, 1);

                Label = new Label(GUI, layout,"", GUI.SmallFont);
                layout.SetComponentPosition(Label, 1, 0, 3, 1);
            }

            public void SetAnnouncement(Announcement announcement)
            {
                ToolTip = announcement.Message;
                Label.Text = announcement.Name;
                Label.TextColor = announcement.Color;
                Icon.Image = announcement.Icon;
            }
        }

        public AnnouncementManager Manager { get; set; }

        public bool IsMaximized { get; set; }
        public Timer WaitTimer { get; set; }
        public int MaxViews { get; set; }

        public List<AnnouncementView> AnnouncementViews { get; set; } 

        public AnnounementViewer(DwarfGUI gui, GUIComponent parent, AnnouncementManager manager) :
            base(gui, parent)
        {
            Manager = manager;

            Manager.OnAdded += Manager_OnAdded;
            Manager.OnRemoved += Manager_OnRemoved;

            IsMaximized = false;


            AnnouncementViews = new List<AnnouncementView>();
            MaxViews = 4;
            WaitTimer = new Timer(5, true);
        }

        void Manager_OnRemoved(Announcement announcement)
        {
            
        }

        void Manager_OnAdded(Announcement announcement)
        {
            AnnouncementView view = new AnnouncementView(GUI, this)
            {
                Mode = Panel.PanelMode.Simple
            };
            AnnouncementViews.Insert(0, view);
            view.SetAnnouncement(announcement);

            if (AnnouncementViews.Count > MaxViews)
            {
                AnnouncementView oldView = AnnouncementViews.ElementAt(AnnouncementViews.Count - 1);
                RemoveChild(oldView);
                AnnouncementViews.RemoveAt(AnnouncementViews.Count - 1);
            }

            WaitTimer.Reset(5);
            UpdateLayout();
        }

        void UpdateLayout()
        {
            int i = 0;
            foreach (AnnouncementView view in AnnouncementViews)
            {
                view.LocalBounds = new Rectangle(0, -(LocalBounds.Height / 2) * i, LocalBounds.Width, LocalBounds.Height / 2 - 7);
                i++;
            }
        }

        public override void Update(DwarfTime time)
        {
            WaitTimer.Update(time);

            if (WaitTimer.HasTriggered)
            {
                if (AnnouncementViews.Count > 0)
                {
                    AnnouncementView view = AnnouncementViews.ElementAt(AnnouncementViews.Count - 1);
                    RemoveChild(view);
                    AnnouncementViews.RemoveAt(AnnouncementViews.Count - 1);
                    UpdateLayout();

                    if (AnnouncementViews.Count > 0)
                    {
                        WaitTimer.Reset(5);
                    }
                }
            }

            base.Update(time);
        }
    }
}
