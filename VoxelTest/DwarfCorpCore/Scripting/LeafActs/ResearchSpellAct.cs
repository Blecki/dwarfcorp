using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class ResearchSpellAct : CreatureAct
    {
        public SpellTree.Node Spell { get; set; }

        public ResearchSpellAct(CreatureAI agent, SpellTree.Node spell) :
            base(agent)
        {
            Spell = spell;
        }

        public override void OnCanceled()
        {
            Creature.OverrideCharacterMode = false;
            base.OnCanceled();
        }

        public override IEnumerable<Act.Status> Run()
        {
            if (Spell.IsResearched)
            {
                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                Creature.OverrideCharacterMode = false;
                yield return Status.Success;
                yield break;
            }
            Timer starParitcle = new Timer(0.5f, false);
            float totalResearch = 0.0f;
            while (!Spell.IsResearched)
            {
                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Creature.OverrideCharacterMode = true;
                float research = Creature.Stats.BuffedInt * 0.25f * Dt;
                Spell.ResearchProgress += research;
                totalResearch += research;
                Creature.Physics.Velocity *= 0;
                if ((int) totalResearch > 0)
                {
                    Creature.Stats.XP += (int)(totalResearch);
                    totalResearch = 0.0f;
                }

                starParitcle.Update(Act.LastTime);
                if(starParitcle.HasTriggered)
                    PlayState.ParticleManager.Trigger("star_particle", Creature.AI.Position, Color.White, 3);
                yield return Status.Running;
            }
            Creature.AI.AddThought(Thought.ThoughtType.Researched);
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
            yield return Status.Success;
            yield break;
        }
    }
}
