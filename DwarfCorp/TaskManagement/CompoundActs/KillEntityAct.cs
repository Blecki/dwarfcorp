using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to an entity, and then hits it until the other entity is dead.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillEntityAct : CompoundCreatureAct
    {
        public GameComponent Entity { get; set; }
        public bool PathExists { get; set; }
        public float RadiusDomain { get; set; }
        public bool Defensive = false;

        public KillEntityAct()
        {
            PathExists = false;
        }

        public bool Verify(CreatureAI creature)
        {
            if (creature.Blackboard.GetData<bool>("NoPath", false))
            {
                if (creature.World.PersistentData.Designations.GetEntityDesignation(Entity, DesignationType.Attack).HasValue(out var designation)
                    && creature.Faction == creature.World.PlayerFaction)
                {
                    creature.World.MakeAnnouncement(String.Format("{0} stopped trying to kill {1} because it is unreachable.", creature.Stats.FullName, Entity.Name));
                    creature.World.TaskManager.CancelTask(designation.Task);
                }

                return false;
            }

            if(Entity == null || Entity.IsDead)
                return false;

            if (RadiusDomain > 0.0)
                if ((creature.Position - Entity.Position).LengthSquared() > RadiusDomain)
                    return false;

            //if (Defensive)
            //{
            //    var ai = Entity.GetRoot().GetComponent<CreatureAI>();
            //    return !ai.Stats.IsFleeing;
            //}


            return true;
        }

        private GameComponent closestDefensiveStructure = null;

        public IEnumerable<Act.Status> OnAttackEnd(CreatureAI creature)
        {
            Verify(creature);
            creature.Creature.OverrideCharacterMode = false;
            creature.Creature.CurrentCharacterMode = CharacterMode.Idle;
            if (closestDefensiveStructure != null)
            {
                closestDefensiveStructure.ReservedFor = null;
            }
            yield return Act.Status.Success;
        }

        public KillEntityAct(GameComponent entity, CreatureAI creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Kill Entity";

            // Get the closest structure that we might defend from.
            closestDefensiveStructure = creature.Faction.OwnedObjects.Where(b => !b.IsReserved && b.Tags.Contains("Defensive")).OrderBy(b => (b.Position - Entity.Position).LengthSquared()).FirstOrDefault();

            // Do not attempt to defend from faraway structures
            if (closestDefensiveStructure != null)
            {
                float distToStructure = (closestDefensiveStructure.Position - creature.Position).Length();
                float distToEntity = (Entity.Position - creature.Position).Length();

                if (distToStructure > 1.5f * distToEntity || distToStructure > 12.0f)
                {
                    closestDefensiveStructure = null;
                }
            }

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
                    new Domain(() => Verify(creature),
                        new Sequence
                        (
                            new AttackAct(Agent, entity)
                        ) | new Wrap(() => OnAttackEnd(creature))
                        );
            }
            else
            {
                if (creature.Creature.Attacks[0].Weapon.Mode != Weapon.AttackMode.Ranged || 
                    closestDefensiveStructure == null || (closestDefensiveStructure.Position - creature.Position).Length() > 20.0f)
                {
                    Tree =
                        new Domain(() => Verify(creature),
                        new Sequence
                            (
                            new GoToEntityAct(entity, creature)
                            {
                                MovingTarget = true,
                                PlanType = planType,
                                Radius = radius
                            } | new Wrap(() => OnAttackEnd(creature)),
                            new AttackAct(Agent, entity),
                            new Wrap(() => OnAttackEnd(creature))
                            ));
                }
                else
                {
                    closestDefensiveStructure.ReservedFor = creature;
                    Tree =
                        new Domain(() => Verify(creature), 
                        new Sequence
                            (
                            new GoToEntityAct(closestDefensiveStructure, creature)
                            {
                                PlanType = PlanAct.PlanType.Into,
                            } | new Wrap(() => OnAttackEnd(creature)),
                            new TeleportAct(Creature.AI) { Location = closestDefensiveStructure.GetRotatedBoundingBox().Center(), Type = TeleportAct.TeleportType.Lerp },
                            new AttackAct(Agent, entity) {  DefensiveStructure = closestDefensiveStructure},
                            new Wrap(() => OnAttackEnd(creature))
                            ));
                }

            }
        }
    }

}