using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class PlagueEvent : ScheduledEvent
    {
        public override void Trigger(WorldManager world)
        {
            DiseaseLibrary.SpreadRandomDiseases(world.PlayerFaction.Minions);
            base.Trigger(world);
        }
    }
}
