using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Scripting.LeafActs;
//using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GoToTaggedObjectAct : CompoundCreatureAct
    {
        public string Tag { get; set; }
        public string ObjectName { get; set; }
        public bool Teleport { get; set; }
        public Vector3 TeleportOffset { get; set; }

        public GoToTaggedObjectAct()
        {
            Name = "Go to tagged object";
            ObjectName = "Tagged Object";
        }

        public GoToTaggedObjectAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Go to tagged object";
            ObjectName = "Tagged Object";
        }


        public IEnumerable<Status> TeleportFunction()
        {
            Body closestItem = Creature.AI.Blackboard.GetData<Body>(ObjectName);

            if (closestItem != null)
            {
                TeleportAct act = new TeleportAct(Creature.AI) { Location = TeleportOffset + closestItem.BoundingBox.Center() };
                act.Initialize();
                foreach (Act.Status status in act.Run())
                {
                    yield return status;
                }

            }

            yield return Status.Fail;
        }

        public override void Initialize()
        {
            if (Teleport)
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectName, Creature.AI),
                        new Wrap(TeleportFunction)
                        );
            }
            else
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectName, Creature.AI)
                        );
            }
            base.Initialize();
        }

 
    }
}
