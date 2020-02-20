using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class ChopEntityAct : CompoundCreatureAct
    {
        public GameComponent Entity;
        
        public ChopEntityAct()
        {
        }

        public bool Verify(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
                return false;

            return Entity != null && !Entity.IsDead;
        }

        public IEnumerable<Act.Status> OnAttackEnd(CreatureAI creature)
        {
            creature.Creature.OverrideCharacterMode = false;
            creature.Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }

        public ChopEntityAct(GameComponent Entity, CreatureAI Creature) :
            base(Creature)
        {
            this.Entity = Entity;
            Name = "Harvest Plant";

            Tree = new Domain(Verify(Agent),
                new Sequence(
                    ActHelper.CreateEquipmentCheckAct(Creature, "Tool", ActHelper.EquipmentFallback.AllowDefault, "Axe", "Pick"), // Allow hands to be used to harvest as a means to prevent game deadlock.
                    new Select(
                        new GoToEntityAct(Entity, Creature)
                        {
                            MovingTarget = false,
                            PlanType = PlanAct.PlanType.Adjacent,
                            Radius = 0.0f
                        },
                        new Wrap(() => OnAttackEnd(Creature))),
                    new AttackAct(Agent, Entity)));
        }
    }
}