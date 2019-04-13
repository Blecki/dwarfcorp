using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class EventLibrary
    {
        public List<ScheduledEvent> Events;

        public EventLibrary()
        {
            Events = FileUtils.LoadJsonListFromMultipleSources<ScheduledEvent>(ContentPaths.events, null, p => p.Name);
        }
    }
}
