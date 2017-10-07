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

        }

        public ResearchSpellTask(string spell)
        {
            Spell = spell;
            Name = "Research " + Spell;
        }

        public override Task Clone()
        {
            return new ResearchSpellTask(Spell);
        }

        public override bool IsFeasible(Creature agent)
        {
            return !agent.World.Master.Spells.GetSpell(Spell).IsResearched;
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
