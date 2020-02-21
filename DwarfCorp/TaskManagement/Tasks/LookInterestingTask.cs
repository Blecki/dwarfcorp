using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class LookInterestingTask : Task
    {
        public LookInterestingTask()
        {
            Name = "Look Interesting";
            Priority = TaskPriority.Eventually;
            BoredomIncrease = GameSettings.Current.Boredom_BoringTask;
            EnergyDecrease = GameSettings.Current.Energy_Tiring;
        }

        public IEnumerable<Act.Status> ConverseFriends(CreatureAI c)
        {
            CreatureAI minionToConverse = null;
            foreach (CreatureAI minion in c.Faction.Minions)
            {
                if (minion == c || minion.Creature.Stats.IsAsleep)
                    continue;

                float dist = (minion.Position - c.Position).Length();

                if (dist < 2 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    minionToConverse = minion;
                    break;
                }
            }
            if (minionToConverse != null)
            {
                c.Converse(minionToConverse);
                Timer converseTimer = new Timer(5.0f, true);
                while (!converseTimer.HasTriggered)
                {
                    converseTimer.Update(DwarfTime.LastTime);
                    yield return Act.Status.Running;
                }
            }
            yield return Act.Status.Success;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return !agent.Stats.IsAsleep ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            if (creature.Stats.IsAsleep)
                return null;

            if (!creature.Faction.Race.HasValue(out var race) || !race.IsIntelligent || !creature.IsOnGround)
                return creature.AI.ActOnWander();
            
            var rooms = creature.World.EnumerateZones();
            var items = creature.Faction.OwnedObjects.OfType<Flag>().ToList();

            bool goToItem = MathFunctions.RandEvent(0.2f);
            if (goToItem && items.Count > 0)
                return new Sequence(
                    new GoToEntityAct(Datastructures.SelectRandom(items), creature.AI),
                    new Wrap(() => ConverseFriends(creature.AI)));

            return creature.AI.ActOnWander();
        }

        public override bool ShouldDelete(Creature agent)
        {
            return agent.IsDead || agent.Stats.IsAsleep || !agent.Active;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }
    }
}
