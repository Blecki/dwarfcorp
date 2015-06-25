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
    public class AnnouncementViewer : GUIComponent
    {

        public class AnnouncementView : Label
        {
            public Color AnnouncementColor { get; set; }
            public AnnouncementView(DwarfGUI gui, GUIComponent parent) :
                base(gui, parent, "", gui.SmallFont)
            {
            }

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

        public AnnouncementManager Manager { get; set; }

        public bool IsMaximized { get; set; }
        public Timer WaitTimer { get; set; }
        public int MaxViews { get; set; }

        public List<AnnouncementView> AnnouncementViews { get; set; }

        public Panel SpeechBubble { get; set; }
        public AnimatedImagePanel Talker { get; set; }
        Animation animation = new Animation(ContentPaths.GUI.dorf_diplo, 64, 64, 0, 1);

        public Timer TweenTimer { get; set; }
        public Timer TweenOutTimer { get; set; }
        public bool TweenState { get; set; }

        public AnnouncementViewer(DwarfGUI gui, GUIComponent parent, AnnouncementManager manager) :
            base(gui, parent)
        {
            TweenTimer = new Timer(0.5f, true);
            TweenOutTimer = new Timer(0.5f, true);
            SpeechBubble = new Panel(gui, this)
            {
                Mode = Panel.PanelMode.SpeechBubble,
                DrawOrder = -2
            };
            SpeechBubble.IsVisible = false;
            Manager = manager;

            Manager.OnAdded += Manager_OnAdded;
            Manager.OnRemoved += Manager_OnRemoved;

            IsMaximized = false;

            AnnouncementViews = new List<AnnouncementView>();
            MaxViews = 4;
            WaitTimer = new Timer(5, true);
            Talker = new AnimatedImagePanel(GUI, this, animation);
            animation.Play();
            animation.Loops = true;
            animation.FrameHZ = 2.0f;
        }

        void Manager_OnRemoved(Announcement announcement)
        {
            
        }

        void Manager_OnAdded(Announcement announcement)
        {
            AnnouncementView view = new AnnouncementView(GUI, this);
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
            float t = -(Easing.CubicEaseInOut(TweenTimer.CurrentTimeSeconds, 0.0f, 1.0f, TweenTimer.TargetTimeSeconds) * 128.0f - 128.0f);

            if (TweenState == false)
            {
                t = Easing.CubicEaseInOut(TweenOutTimer.CurrentTimeSeconds, 0.0f, 1.0f, TweenOutTimer.TargetTimeSeconds) * 128.0f;
            }
            List<Rectangle> rects = new List<Rectangle>();
            int i = 0;
            foreach (AnnouncementView view in AnnouncementViews)
            {
                view.LocalBounds = new Rectangle(0, -(LocalBounds.Height / 2) * i + (int)(t), LocalBounds.Width, LocalBounds.Height / 2 - 7);
                rects.Add(view.LocalBounds);
                i++;
            }
            if (AnnouncementViews.Count > 0)
            {
                SpeechBubble.LocalBounds = MathFunctions.GetBoundingRectangle(rects);
                Talker.LocalBounds = new Rectangle(SpeechBubble.LocalBounds.X - 128, (int)t - 64, 128, 128);
            }
            else
            {
                Talker.LocalBounds = new Rectangle(-128, (int)t - 64, 128, 128);
            }
           
        }

        public override void Update(DwarfTime time)
        {
            TweenState = AnnouncementViews.Count > 0;
            WaitTimer.Update(time);
            animation.Update(time);
            if (WaitTimer.HasTriggered)
            {
                if (AnnouncementViews.Count > 0)
                {
                    AnnouncementView view = AnnouncementViews.ElementAt(AnnouncementViews.Count - 1);
                    RemoveChild(view);
                    AnnouncementViews.RemoveAt(AnnouncementViews.Count - 1);
                    if (AnnouncementViews.Count > 0)
                    {
                        WaitTimer.Reset();
                    }
                    else
                    {
                        TweenTimer.Reset();
                        TweenOutTimer.Reset();
                        TweenState = true;
                    }
                }
            }


            if (!SpeechBubble.IsVisible && AnnouncementViews.Count > 0)
            {
                TweenTimer.Reset();
            }
            
            if (SpeechBubble.IsVisible && AnnouncementViews.Count == 0)
            {
                TweenOutTimer.Reset();
            }

            if(TweenState)
                TweenTimer.Update(time);
            else
                TweenOutTimer.Update(time);



            SpeechBubble.IsVisible = AnnouncementViews.Count > 0;
            Talker.IsVisible = SpeechBubble.IsVisible || TweenState == false;

            if (SpeechBubble.IsVisible || Talker.IsVisible)
            {
                UpdateLayout();
            }

            base.Update(time);
        }
    }
}
