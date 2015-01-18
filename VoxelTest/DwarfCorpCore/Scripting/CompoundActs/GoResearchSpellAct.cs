using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GoResearchSpellAct : CompoundCreatureAct
    {
        public SpellTree.Node Spell { get; set; }
        public GoResearchSpellAct()
        {

        }


        public GoResearchSpellAct(CreatureAI creature, SpellTree.Node node) :
            base(creature)
        {
            Spell = node;
            Name = "Research spell " + node.Spell.Name;
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(() => Creature.Unreserve("Research"));
            Tree = new Sequence(
                new Wrap(() => Creature.FindAndReserve("Research", "Research")),
                new Sequence
                    (
                        new GoToTaggedObjectAct(Agent) { Tag = "Research", Teleport = false, TeleportOffset = new Vector3(1, 0, 0), ObjectName = "Research" },
                        new ResearchSpellAct( Agent, Spell),
                        unreserveAct
                    ) | new Sequence(unreserveAct, false)
                    ) | new Sequence(unreserveAct, false);
            base.Initialize();
        }


        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("Research"))
            {
                continue;
            }
            base.OnCanceled();
        }


    }
}