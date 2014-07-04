using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Announce(string title, string message)
        {
            AddAnnouncement(new Announcement
            {
                Color = Color.White,
                Icon = null,
                Message = message,
                Name = title
            });
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
        }

    }
}
