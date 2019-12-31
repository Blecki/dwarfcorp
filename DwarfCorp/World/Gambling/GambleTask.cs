using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting
{
    public class GambleTask : Task
    {
        public GambleTask()
        {
            Name = "Gamble";
            ReassignOnDeath = false;
            BoredomIncrease = GameSettings.Current.Boredom_Gamble;
            EnergyDecrease = GameSettings.Current.Energy_Arduous;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.Stats.IsAsleep || agent.IsDead || agent.Stats.Money < 10.0m || agent.World.GamblingState.Participants.Count > 4 || agent.Stats.Boredom.IsSatisfied())
                return Feasibility.Infeasible;
            return Feasibility.Feasible;
        }

        public override bool IsComplete(WorldManager World)
        {
            return World.GamblingState.State == Gambling.Status.Ended;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override MaybeNull<Act> CreateScript(Creature agent)
        {
            return new GambleAct() { Agent = agent.AI, Game = agent.World.GamblingState };
        }
    }
}
