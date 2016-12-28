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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     The announcement manager handles sending messages to the player on events.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class AnnouncementManager
    {
        /// <summary>
        ///     Delegate called whenever an announcement is added to the view.
        /// </summary>
        /// <param name="announcement">The announcement to add.</param>
        public delegate void AnnouncementAdded(Announcement announcement);

        /// <summary>
        ///     Delegate called whenever an announcement is removed from view.
        /// </summary>
        /// <param name="announcement">The announcement to remove.</param>
        public delegate void AnnouncementRemoved(Announcement announcement);

        public AnnouncementManager()
        {
            Announcements = new List<Announcement>();
            MaxAnnouncements = 32;
        }

        /// <summary>
        ///     Maximum number of announcements displayed to the user at any time.
        /// </summary>
        public int MaxAnnouncements { get; set; }

        /// <summary>
        ///     List of all announcements displayed to the user.
        /// </summary>
        public List<Announcement> Announcements { get; set; }

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

        /// <summary>
        ///     Creates a new Announcement with te gven title and message.
        /// </summary>
        /// <param name="title">The short text displayed to the user.</param>
        /// <param name="message">Longer text displayed to the user whenever they mouse over the message.</param>
        /// <param name="clickAction">Delegate to call whenever an announcement is clicked.</param>
        public void Announce(string title, string message, Announcement.Clicked clickAction = null)
        {
            var toAdd = new Announcement
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

        /// <summary>
        ///     Add an announcement to the list. Removes old announcements that are no longer relevant.
        /// </summary>
        /// <param name="announcement">The announcement to add.</param>
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
        }
    }
}