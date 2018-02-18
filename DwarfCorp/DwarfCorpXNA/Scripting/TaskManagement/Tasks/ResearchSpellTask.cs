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
      
        public ResearchSpellTask()
        {
            Category = TaskCategory.Research;
        }

        public ResearchSpellTask(string spell)
        {
            Category = TaskCategory.Research;
            Spell = spell;
            Name = "Research " + Spell;
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

        public override void SetupScript(Creature agent)
        {
            Script = new GoResearchSpellAct(agent.AI, agent.World.Master.Spells.GetSpell(Spell));
        }
    }
}
