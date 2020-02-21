using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class RechargeObjectAct : CompoundCreatureAct
    {
        public MagicalObject Entity { get; set; }
        public bool PathExists { get; set; }

        public RechargeObjectAct()
        {
            PathExists = false;
        }

        public bool Verify(CreatureAI creature)
        {
            return Entity != null && !Entity.IsDead && Entity.CurrentCharges < Entity.MaxCharges;
        }

        public IEnumerable<Act.Status> OnRechargeEnd(CreatureAI creature)
        {
            creature.Creature.OverrideCharacterMode = false;
            creature.Creature.CurrentCharacterMode = CharacterMode.Idle;
            creature.Creature.Physics.Orientation = Physics.OrientMode.RotateY;
            creature.Physics.Active = true;
            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> Recharge()
        {
            foreach (var status in Agent.Creature.HitAndWait(true,
                () => { return Entity.MaxCharges; },
                () => { return Entity.CurrentCharges; },
                () => { Entity.CurrentCharges++; }, () => { return (Entity.GetRoot() as GameComponent).Position; }))
            {
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

        public RechargeObjectAct(MagicalObject entity, CreatureAI creature) :
            base(creature)
        {
            Entity = entity;
            Name = "Recharge Object";
            PlanAct.PlanType planType = PlanAct.PlanType.Adjacent;
            Tree = new Select(
                new Domain(Verify(creature), 
                    new Sequence(
                        new Select(
                            new GoToEntityAct(Entity.GetRoot() as GameComponent, creature)
                            {
                                MovingTarget = true,
                                PlanType = planType,
                                Radius = 2.0f
                            },
                            new Wrap(() => OnRechargeEnd(creature))),
                        new Wrap(Recharge) { Name = "Recharge Object" },
                        new Wrap(() => OnRechargeEnd(creature)))),
                new Wrap(() => OnRechargeEnd(creature)));
        }

        public override void OnCanceled()
        {
            foreach(var status in OnRechargeEnd(Creature.AI))
            {
                continue;
            }
            base.OnCanceled();
        }
    }

}