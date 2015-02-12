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
    public class KillEntityTask : Task
    {
        public enum KillType
        {
            Chop,
            Attack,
            Auto
        }
        public Body EntityToKill = null;
        public KillType Mode { get; set; }

        public KillEntityTask(Body entity, KillType type)
        {
            Mode = type;
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = PriorityType.Urgent;
        }

        public override Task Clone()
        {
            return new KillEntityTask(EntityToKill, Mode);
        }

        public override Act CreateScript(Creature creature)
        {
            return new KillEntityAct(EntityToKill, creature.AI, Mode);
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
                if (EntityToKill.IsDead) return false;

                Creature ai = EntityToKill.GetChildrenOfTypeRecursive<Creature>().FirstOrDefault();
                switch (Mode)
                {
                    case KillType.Attack:
                    {
                        if (!agent.Faction.AttackDesignations.Contains(EntityToKill)) return false;
                        return true;
                        break;
                    }
                    case KillType.Chop:
                    {
                        if (!agent.Faction.ChopDesignations.Contains(EntityToKill))
                        {
                            return false;
                        }
                        return true;
                        break;
                    }
                    case KillType.Auto:
                    {
                        return true;
                    }
                }


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