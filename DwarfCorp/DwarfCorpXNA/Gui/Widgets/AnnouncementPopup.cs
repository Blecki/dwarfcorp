using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Gui.Widgets
{
    public class QueuedAnnouncement
    {
        public String Text;
        public Action<Gui.Root, QueuedAnnouncement> ClickAction;
        public Func<bool> ShouldKeep;
        public bool Keep;
        public double SecondsVisible;
        public Widget Widget = null;
    }

    public class AnnouncementPopup : Gui.Widget
    {
        public List<QueuedAnnouncement> Announcements = new List<QueuedAnnouncement>();
        private List<QueuedAnnouncement> NewAnnouncements = new List<QueuedAnnouncement>();
        public String Speaker = "dwarf";
        public double MessageLingerSeconds = 5.0f;

        public void QueueAnnouncement(QueuedAnnouncement Announcement)
        {
            lock (NewAnnouncements)
            {
                if (Announcements.All(msg => msg.Text != Announcement.Text) 
                    && NewAnnouncements.All(msg => msg.Text != Announcement.Text))
                {
                    Announcement.Keep = true;
                    Announcement.SecondsVisible = 0;
                    NewAnnouncements.Add(Announcement);
                }
            }
        }

        public override void Construct()
        {
            Hidden = true;
            Font = "font10";
            OnUpdate += (sender, time) =>
                {
                    lock (NewAnnouncements)
                    {
                        foreach (var announcement in NewAnnouncements)
                        {
                            var announcement1 = announcement;
                            announcement.Widget = AddChild(new Widget
                            {
                                Text = announcement.Text,
                                ChangeColorOnHover = true,
                                OnClick = (_s, _a) =>
                                {
                                    if (announcement1.ClickAction != null)
                                        announcement1.ClickAction(Root, announcement1);
                                }
                            });

                            announcement.Widget.AddChild(new Widget
                            {
                                Background = new TileReference("round-buttons", 5),
                                AutoLayout = AutoLayout.FloatTopRight,
                                MaximumSize = new Point(16, 16),
                                MinimumSize = new Point(16, 16),
                                OnClick = (_s, _a) =>
                                {
                                    announcement.Keep = false;
                                },
                                OnLayout = (_s) =>
                                {
                                    _s.Rect.X += 16;
                                }
                            });

                            Announcements.Add(announcement);
                        }

                        NewAnnouncements.Clear();
                    }

                    foreach (var announcement in Announcements)
                    {
                        if (announcement.Keep)
                        {
                            if (announcement.ShouldKeep != null)
                                announcement.Keep = announcement.ShouldKeep();
                            else
                            {
                                announcement.SecondsVisible += time.ElapsedGameTime.TotalSeconds;
                                announcement.Keep = announcement.SecondsVisible < MessageLingerSeconds;
                            }
                        }
                    }

                    lock (NewAnnouncements)
                    {
                        Announcements.RemoveAll(a => a.Keep == false);
                    }

                    Children = Announcements.Select(a => a.Widget).ToList();

                    Hidden = (Announcements.Count == 0);
                    Invalidate();
                };

            Root.RegisterForUpdate(this);
            base.Construct();
        }


        protected override Gui.Mesh Redraw()
        {
            var meshes = new List<Gui.Mesh>();

            var speakerTiles = Root.GetTileSheet(Speaker);

            meshes.Add(Gui.Mesh.Quad()
                .TileScaleAndTexture(speakerTiles, 0)
                .Translate(Rect.Right - speakerTiles.TileWidth, Rect.Bottom - speakerTiles.TileHeight));

            var font = Root.GetTileSheet(Font) as VariableWidthFont;

            foreach (var announcement in Announcements)
            {
                announcement.Widget.Text = font.WordWrapString(announcement.Text, 1.0f, Rect.Width - (speakerTiles.TileWidth * 2));
                var size = font.MeasureString(announcement.Widget.Text);
                announcement.Widget.Rect = new Rectangle(0, 0, Rect.Width - (speakerTiles.TileWidth * 2), size.Y);
            }

            // Resize widget.
            var totalSize = Announcements.Select(a => a.Widget.Rect.Height + 2).Sum() - 2;
            var newParentSize = totalSize + (speakerTiles.TileHeight / 2) + 60;
            Rect = new Rectangle(Rect.X, Rect.Bottom - newParentSize, Rect.Width, newParentSize);

            var childPos = Rect.Y + 20;
            foreach (var announcement in Announcements)
            {
                announcement.Widget.Rect = new Rectangle(Rect.X + 20, childPos, announcement.Widget.Rect.Width,
                    announcement.Widget.Rect.Height);
                announcement.Widget.Layout();
                childPos += announcement.Widget.Rect.Height + 2;
            }

            var bubbleRect = new Rectangle(Rect.Left, Rect.Top,
                Rect.Width - speakerTiles.TileWidth, Rect.Height - (speakerTiles.TileHeight / 2));
            meshes.Add(Gui.Mesh.CreateScale9Background(bubbleRect, Root.GetTileSheet("speech-bubble")));

            return Gui.Mesh.Merge(meshes.ToArray());
        }
    }
}
