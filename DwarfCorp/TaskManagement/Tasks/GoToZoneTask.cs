using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    public class GoToZoneTask : Task
    {
        public Zone Zone;
        public bool Wait;

        public GoToZoneTask()
        {

        }

        public GoToZoneTask(Zone zone)
        {
            Zone = zone;
            Category = TaskCategory.Other;
            Priority = TaskPriority.Medium;
            ReassignOnDeath = false;
            Name = "Go to " + Zone.ID;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            if (!Wait)
                return new GoToZoneAct(agent.AI, Zone);

            return new Sequence(
                new GoToZoneAct(agent.AI, Zone),
                new Wait(agent.AI, 999) { Name = "Wait." });
        }
    }
}