using System;
using DwarfCorp.Gui.Widgets;
using Color = Microsoft.Xna.Framework.Color;

namespace DwarfCorp
{
    public partial class WorldManager : IDisposable
    {
        public void MakeAnnouncement(String Message, Action<Gui.Root, QueuedAnnouncement> ClickAction = null, Func<bool> Keep = null, bool logEvent = true, String eventDetails = "")
        {
            MakeAnnouncement(Message, Color.Black, ClickAction, Keep, logEvent, eventDetails);
        }

        public void MakeAnnouncement(String Message, Color eventColor, Action<Gui.Root, QueuedAnnouncement> ClickAction = null, Func<bool> Keep = null, bool logEvent = true, String eventDetails = "")
        {
            if (OnAnnouncement != null)
                OnAnnouncement(new QueuedAnnouncement
                {
                    Text = Message,
                    ClickAction = ClickAction,
                    ShouldKeep = Keep
                });

            if (logEvent)
            {
                LogEvent(Message, eventColor, eventDetails);
            }
        }

        public void MakeAnnouncement(QueuedAnnouncement Announcement)
        {
            LogEvent(Announcement.Text);
            if (OnAnnouncement != null)
                OnAnnouncement(Announcement);
        }
    }
}
