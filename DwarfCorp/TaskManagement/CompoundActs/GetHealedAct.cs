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
            var closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true, Agent);

            if (closestItem != null && !Creature.Stats.Health.IsCritical())
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                var unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree = new Select(
                    new Sequence(
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) { Location = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) },
                        new SleepAct(Creature.AI) { HealRate = 1.0f, RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f), Type = SleepAct.SleepType.Heal },
                        unreserveAct),
                    unreserveAct);
            }
            else
            {
                if (Agent.Faction == Agent.World.PlayerFaction)
                {
                    Agent.World.UserInterface.MakeWorldPopup(String.Format("{0} passed out.", Agent.Stats.FullName), Agent.Physics, -10, 10);
                    Agent.World.TaskManager.AddTask(new HealAllyTask(Agent) { Priority = TaskPriority.High });
                }
                Tree = new SleepAct(Creature.AI) { HealRate = 0.4f, RechargeRate = 1.0f, Teleport = false, Type = SleepAct.SleepType.Heal };
            }
            base.Initialize();
        }
    }
}
