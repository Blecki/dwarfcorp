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


        public override void Initialize()
        {
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            Body closestItem = Agent.Blackboard.GetData<Body>(ObjectName);
            if (closestItem != null)
            {
                Act unreserveAct = new Wrap(() => Creature.Unreserve(ObjectName));

                if (Teleport)
                {
                    Tree =
                   new Sequence
                   (
                       new SetBlackboardData<Body>(Creature.AI, ObjectName, closestItem),
                       new GoToEntityAct(closestItem, Creature.AI),
                       new TeleportAct(Creature.AI) { Location = TeleportOffset + closestItem.BoundingBox.Center() }
                   ) | unreserveAct;
                }
                else
                {
                    Tree =
                    new Sequence
                    (
                       new SetBlackboardData<Body>(Creature.AI, ObjectName, closestItem),
                       new GoToEntityAct(closestItem, Creature.AI)
                    ) | unreserveAct;
                }
            }
            else
            {
                Tree = null;
            }
            return base.Run();
        }
    }
}
