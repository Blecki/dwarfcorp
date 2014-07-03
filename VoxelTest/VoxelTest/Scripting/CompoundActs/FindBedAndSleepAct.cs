using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Scripting.LeafActs;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item with the tag "bed", goes to it, and sleeps in it.
    /// </summary>
    public class FindBedAndSleepAct : CompoundCreatureAct
    {

        public FindBedAndSleepAct()
        {
            Name = "Find bed and sleep";
        }

        public FindBedAndSleepAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Find bed and sleep";
        }

        public IEnumerable<Status> UnReserve(Item item)
        {
            item.ReservedFor = null;
            yield return Status.Success;
        }

        public override void Initialize()
        {
            Item closestItem = Agent.Faction.FindNearestItemWithTags(new TagList("Bed"), Agent.Position, true);

            
            if (Agent.Status.Energy.IsUnhappy() && closestItem != null)
            {
                closestItem.ReservedFor = Agent;
                Act unreserveAct = new Wrap(() => UnReserve(closestItem));
                Tree = 
                    new Sequence
                    (
                        new GoToEntityAct(closestItem.UserData, Creature.AI),
                        new SleepAct(Creature.AI) { RechargeRate = 10.0f, Teleport = true, TeleportLocation = closestItem.UserData.BoundingBox.Center() + new Vector3(-0.0f, 0.2f, -0.0f)},
                        unreserveAct
                    ) | unreserveAct;
            }
            else if(Agent.Status.Energy.IsUnhappy() && closestItem == null)
            {
                Tree = new SleepAct(Creature.AI)
                {
                    RechargeRate = 1.0f
                };
            }
            else
            {
                Tree = null;
            }
            base.Initialize();
        }
    }
}
