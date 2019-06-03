using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class ChopEntityAct : CompoundCreatureAct
    {
        public GameComponent Entity { get; set; }
        public bool PathExists { get; set; }
        
        public ChopEntityAct()
        {
            PathExists = false;
        }

        public bool Verify(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                var designation = creature.Faction.Designations.GetEntityDesignation(Entity, DesignationType.Chop);
                if (designation != null)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} cancelled harvest task because it is unreachable", creature.Stats.FullName));
                    if (creature.Faction == creature.World.PlayerFaction)
                        creature.World.TaskManager.CancelTask(designation.Task);
                }
                return false;
            }

            return Entity != null && !Entity.IsDead;
        }

        public IEnumerable<Act.Status> OnAttackEnd(CreatureAI creature)
        {
            Verify(creature);
            creature.Creature.OverrideCharacterMode = false;
            creature.Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }

        public ChopEntityAct(GameComponent entity, CreatureAI creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Harvest Plant";
            PlanAct.PlanType planType = PlanAct.PlanType.Adjacent;
            float radius = 0.0f;
            if (creature.Creature.Attacks[0].Weapon.Mode == Weapon.AttackMode.Ranged)
            {
                planType = PlanAct.PlanType.Radius;
                radius = creature.Creature.Attacks[0].Weapon.Range;
            }
            if (creature.Movement.IsSessile)
            {
                Tree =
                    new Domain(Verify(Agent),
                        new Sequence
                        (
                            new MeleeAct(Agent, entity)
                        ) | new Wrap(() => OnAttackEnd(creature)) | Verify(Agent)
                        );
            }
            else
            {
                Tree =
                    new Domain(Verify(Agent), 
                    new Sequence
                        (
                        new GoToEntityAct(entity, creature)
                        {
                            MovingTarget = false,
                            PlanType = planType,
                            Radius = radius
                        } | new Wrap(() => OnAttackEnd(creature)),
                        new MeleeAct(Agent, entity)
                        ) | Verify(Agent));

            }
        }
    }

}