using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should kill an entity.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class KillEntityTask : Task
    {
        public Body EntityToKill = null;

        public KillEntityTask(Body entity)
        {
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = PriorityType.Urgent;
        }

        public override Task Clone()
        {
            return new KillEntityTask(EntityToKill);
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillEntityAct(EntityToKill, creature.AI);
        }

        public override float ComputeCost(Creature agent)
        {
            if (agent == null || EntityToKill == null)
            {
                return 10000;
            }

            else return (agent.AI.Position - EntityToKill.LocalTransform.Translation).LengthSquared() * 0.01f;
        }

        public override bool IsFeasible(Creature agent)
        {
            if(EntityToKill == null)
            {
                return false;
            }
            else
            {
                Creature ai = EntityToKill.GetChildrenOfTypeRecursive<Creature>().FirstOrDefault();

                if(ai == null)
                {
                    return true;
                }

                Relationship relation = Alliance.Relationships[new Alliance.AlliancePair
                {AllianceA =  ai.Allies, AllianceB = agent.Allies}];

                return relation == Relationship.Hates || relation == Relationship.Indifferent;
            }
        }
    }

}