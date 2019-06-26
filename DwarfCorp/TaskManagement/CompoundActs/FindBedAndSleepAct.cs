using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        public override void OnCanceled()
        {
            foreach(var status in Creature.Unreserve("Bed"))
            {

            }
            base.OnCanceled();
        }

        public override void Initialize()
        {
            GameComponent closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true, Agent);
            Zone closestZone = Agent.World.FindNearestZone(Agent.Position);
           
            if (!Agent.Stats.Energy.IsSatisfied() && closestItem != null)
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                closestItem.ReservedFor = Agent;
                Act unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree = 
                    new Sequence
                    (
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) {Location = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f)},
                        new SleepAct(Creature.AI) { RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) },
                        unreserveAct
                    ) | unreserveAct;
            }
            else if (!Agent.Stats.Energy.IsSatisfied() && closestItem == null && closestZone != null)
            {
                Creature.AddThought("I slept on the ground.", new TimeSpan(0, 8, 0, 0), -6.0f);

                Tree = new Sequence(new GoToZoneAct(Creature.AI, closestZone),
                                    new SleepAct(Creature.AI)
                                    {
                                        RechargeRate = 1.0f
                                    });
            }
            else if (!Agent.Stats.Energy.IsSatisfied() && closestItem == null && closestZone == null)
            {
                Creature.AddThought("I slept on the ground.", new TimeSpan(0, 8, 0, 0), -6.0f);

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

    /// <summary>
    /// A creature finds an item with the tag "bed", goes to it, and sleeps in it.
    /// </summary>
    public class GetHealedAct : CompoundCreatureAct
    {

        public GetHealedAct()
        {
            Name = "Heal thyself";
        }

        public GetHealedAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Heal thyself";
        }

        public override void OnCanceled()
        {
            foreach (var status in Creature.Unreserve("Bed"))
            {

            }
            base.OnCanceled();
        }

        public override void Initialize()
        {
            GameComponent closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true, Agent);


            if (closestItem != null && !Creature.Stats.Health.IsCritical())
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                Act unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree =
                    new Sequence
                    (
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) { Location = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) },
                        new SleepAct(Creature.AI) { HealRate = 1.0f, RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) , Type = SleepAct.SleepType.Heal},
                        unreserveAct
                    ) | unreserveAct;
            }
            else
            {
                if (Agent.Faction == Agent.World.PlayerFaction)
                {
                    Agent.World.UserInterface.MakeWorldPopup(String.Format("{0} passed out.", Agent.Stats.FullName), Agent.Physics, -10, 10);
                    Agent.World.TaskManager.AddTask(new HealAllyTask(Agent) { Priority = TaskPriority.High });
                }
                Tree = new SleepAct(Creature.AI) { HealRate = 0.1f, RechargeRate = 1.0f, Teleport = false, Type = SleepAct.SleepType.Heal };
            }
            base.Initialize();
        }
    }
}
