// AnnouncementManager.cs
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
using System.Runtime.Remoting.Messaging;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class AnnouncementManager
    {
        public delegate void AnnouncementAdded(Announcement announcement);
        public delegate void AnnouncementRemoved(Announcement announcement);

        public event AnnouncementAdded OnAdded;

        protected virtual void OnOnAdded(Announcement announcement)
        {
            AnnouncementAdded handler = OnAdded;
            if (handler != null) handler(announcement);
        }

        public event AnnouncementRemoved OnRemoved;

        protected virtual void OnOnRemoved(Announcement announcement)
        {
            AnnouncementRemoved handler = OnRemoved;
            if (handler != null) handler(announcement);
        }

        public int MaxAnnouncements { get; set; }
        public List<Announcement> Announcements { get; set; }

        public AnnouncementManager()
        {
            Announcements = new List<Announcement>();
            MaxAnnouncements = 32;
        }

        public void Announce(string title, string message, Announcement.Clicked clickAction = null)
        {
            Announcement toAdd = new Announcement
            {
                Color = Color.Black,
                Icon = null,
                Message = message,
                Name = title
            };

            if (clickAction != null)
            {
                toAdd.OnClicked += clickAction;
            }

            AddAnnouncement(toAdd);
        }

        public void AddAnnouncement(Announcement announcement)
        {
            Announcements.Add(announcement);

            if (Announcements.Count > MaxAnnouncements)
            {
                Announcement toRemove = Announcements.ElementAt(0);
                Announcements.RemoveAt(0);
                OnRemoved(toRemove);
            }

            OnAdded(announcement);

            if (Announcements.Count > 25)
            {
                Announcements.RemoveAt(0);
            }
        }

    }
}
