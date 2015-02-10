using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GoTrainAct : CompoundCreatureAct
    {
        public GoTrainAct()
        {

        }


        public GoTrainAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Train";
        }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(() => Creature.Unreserve("Train"));
            Tree = new Sequence(
                new Wrap(() => Creature.FindAndReserve("Train", "Train")),
                new Sequence
                    (
                        new GoToTaggedObjectAct(Agent) { Tag = "Train", Teleport = false, TeleportOffset = new Vector3(1, 0, 0), ObjectName = "Train" },
                        new MeleeAct(Agent, "Train") {Training = true, Timeout = new Timer(10.0f, false)},
                        unreserveAct
                    ) | new Sequence(unreserveAct, false)
                    ) | new Sequence(unreserveAct, false);
            base.Initialize();
        }


        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("Train"))
            {
                continue;
            }
            base.OnCanceled();
        }


    }
}