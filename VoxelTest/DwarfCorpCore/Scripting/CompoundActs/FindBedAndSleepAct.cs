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


        public override void Initialize()
        {
            Body closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true);

            
            if (Agent.Status.Energy.IsUnhappy() && closestItem != null)
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                Act unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree = 
                    new Sequence
                    (
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) {Location = closestItem.BoundingBox.Center() + new Vector3(-0.0f, 0.2f, -0.0f)},
                        new SleepAct(Creature.AI) { RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.BoundingBox.Center() + new Vector3(-0.0f, 0.2f, -0.0f)},
                        unreserveAct
                    ) | unreserveAct;
            }
            else if(Agent.Status.Energy.IsUnhappy() && closestItem == null)
            {
                Creature.AI.AddThought(Thought.ThoughtType.SleptOnGround);

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
