using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    class ResearchSpellTask : Task
    {
        public string Spell { get; set; }
        private WorldManager World { get; set; }
        public ResearchSpellTask()
        {
            Category = TaskCategory.Research;
            MaxAssignable = 10;
            BoredomIncrease = 0.1f;
        }

        public ResearchSpellTask(string spell)
        {
            Category = TaskCategory.Research;
            Spell = spell;
            Name = "Research " + Spell;
            MaxAssignable = 10;
            BoredomIncrease = 0.1f;
        }


        private IEnumerable<Act.Status> Cleanup(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled research task because research station was unreachable.", creature.Stats.FullName));
                    creature.World.Master.TaskManager.CancelTask(this);
                }
                yield return Act.Status.Fail;
                yield break;
            }
            yield return Act.Status.Success;
        }


        public override Feasibility IsFeasible(Creature agent)
        {
            if (!agent.Stats.IsTaskAllowed(Task.TaskCategory.Research))
                return Feasibility.Infeasible;

            return !agent.World.Master.Spells.GetSpell(Spell).IsResearched ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return !agent.World.Master.Spells.GetSpell(Spell).IsResearched;
        }

        public override Act CreateScript(Creature agent)
        {
            return ((new GoResearchSpellAct(agent.AI, agent.World.Master.Spells.GetSpell(Spell))) | new Wrap(() => Cleanup(agent.AI)) & new Wrap(() => Cleanup(agent.AI)));
        }

        public override bool IsComplete(Faction faction)
        {
            return faction.World.Master.Spells.GetSpell(Spell).IsResearched;
        }

    }
}
