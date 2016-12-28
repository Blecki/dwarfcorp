// AnnouncementViewer.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Displays the Announcements stored in the Announcement Manager to the player. The player can
    ///     click on these announcements to trigger actions.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class AnnouncementViewer : GUIComponent
    {
        /// <summary>
        ///     Animation associated with the talking creature.
        /// </summary>
        private readonly Animation animation = new Animation(ContentPaths.GUI.dorf_diplo, 64, 64, 0, 1);

        /// <summary>
        ///     Create an announcement viewer.
        /// </summary>
        /// <param name="gui">The GUI.</param>
        /// <param name="parent">Viewer is attached to this widget.</param>
        /// <param name="manager">Displays announcements from this manager.</param>
        public AnnouncementViewer(DwarfGUI gui, GUIComponent parent, AnnouncementManager manager) :
            base(gui, parent)
        {
            SpeechBubble = new Panel(gui, this)
            {
                Mode = Panel.PanelMode.SpeechBubble,
                DrawOrder = -2,
                IsVisible = false
            };
            Manager = manager;

            Manager.OnAdded += Manager_OnAdded;
            Manager.OnRemoved += Manager_OnRemoved;

            IsMaximized = false;

            AnnouncementViews = new List<AnnouncementView>();
            MaxViews = 4;
            WaitTimer = new Timer(5, true);
            Talker = new AnimatedImagePanel(GUI, this, animation)
            {
                LocalBounds = new Rectangle(-128, -48, 128, 128)
            };
            animation.Play();
            animation.Loops = true;
            animation.FrameHZ = 2.0f;
        }

        /// <summary>
        ///     Keeps track of a list of announcements.
        /// </summary>
        public AnnouncementManager Manager { get; set; }


        /// <summary>
        ///     If true, the announcements are visible.
        /// </summary>
        public bool IsMaximized { get; set; }

        /// <summary>
        ///     The time to wait until the announcements are minimized.
        /// </summary>
        public Timer WaitTimer { get; set; }

        /// <summary>
        ///     Maximum number of announcements to display when maximized.
        /// </summary>
        public int MaxViews { get; set; }

        /// <summary>
        ///     The list of widgets displaying announcements.
        /// </summary>
        public List<AnnouncementView> AnnouncementViews { get; set; }

        /// <summary>
        ///     Speech bubble to display behind the announcements.
        /// </summary>
        public Panel SpeechBubble { get; set; }

        /// <summary>
        ///     Animation of a talking creature to dispaly beneath the announcements.
        /// </summary>
        public AnimatedImagePanel Talker { get; set; }

        /// <summary>
        ///     Called whenever the manager removes an announcement.
        /// </summary>
        /// <param name="announcement">Announcement that was removed.</param>
        private void Manager_OnRemoved(Announcement announcement)
        {
        }

        /// <summary>
        ///     Called whenever the manager adds an announcement.
        /// </summary>
        /// <param name="announcement">Announcement that was added.</param>
        private void Manager_OnAdded(Announcement announcement)
        {
            var view = new AnnouncementView(GUI, this);
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

        /// <summary>
        ///     Figure out where exactly all of the announcement views should be placed relative to the widget.
        /// </summary>
        private void UpdateLayout()
        {
            var rects = new List<Rectangle>();
            int i = 0;
            foreach (AnnouncementView view in AnnouncementViews)
            {
                view.LocalBounds = new Rectangle(0, -(LocalBounds.Height/4)*i, LocalBounds.Width,
                    LocalBounds.Height/4 - 7);
                rects.Add(view.LocalBounds);
                i++;
            }
            if (AnnouncementViews.Count > 0)
            {
                SpeechBubble.LocalBounds = MathFunctions.GetBoundingRectangle(rects);
            }
        }

        public override void Update(DwarfTime time)
        {
            WaitTimer.Update(time);
            animation.Update(time);

            // If the wait timer was triggered, remove one announcement.
            if (WaitTimer.HasTriggered)
            {
                if (AnnouncementViews.Count > 0)
                {
                    TriggerMouseOver = true;
                    AnnouncementView view = AnnouncementViews.ElementAt(AnnouncementViews.Count - 1);
                    RemoveChild(view);
                    AnnouncementViews.RemoveAt(AnnouncementViews.Count - 1);
                    if (AnnouncementViews.Count > 0)
                    {
                        WaitTimer.Reset();
                    }
                    else
                    {
                        // Remove the talker if the announcements are empty.
                        Talker.TweenOut(Drawer2D.Alignment.Bottom);
                    }
                }
                else
                {
                    TriggerMouseOver = false;
                }
            }

            // If there are new announcements and the talker is not around, tween him in.
            if (AnnouncementViews.Count > 0 && !Talker.IsVisible)
            {
                Talker.TweenIn(Drawer2D.Alignment.Bottom);
            }

            SpeechBubble.IsVisible = AnnouncementViews.Count > 0;

            if (SpeechBubble.IsVisible || Talker.IsVisible)
            {
                UpdateLayout();
            }

            base.Update(time);
        }

        /// <summary>
        ///     A GUI widget for a specific announcement. Consists of text with a tooltip and click delegate.
        /// </summary>
        public class AnnouncementView : Label
        {
            public AnnouncementView(DwarfGUI gui, GUIComponent parent) :
                base(gui, parent, "", gui.SmallFont)
            {
            }

            public Color AnnouncementColor { get; set; }

            public void SetAnnouncement(Announcement announcement)
            {
                AnnouncementColor = announcement.Color;
                ToolTip = announcement.Message;
                Text = announcement.Name;
                TextColor = announcement.Color;

                OnClicked += announcement.ActivateClick;
            }

            public override void Update(DwarfTime time)
            {
                if (IsMouseOver)
                {
                    TextColor = Color.DarkRed;
                }
                else
                {
                    TextColor = AnnouncementColor;
                }
                base.Update(time);
            }
        }
    }
}