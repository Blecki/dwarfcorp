// KillEntityAct.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            Tree =
                new Domain(Verify(creature), new Sequence
                    (
                    new GoToEntityAct(Entity.GetRoot() as GameComponent, creature)
                    {
                        MovingTarget = true,
                        PlanType = planType,
                        Radius = 2.0f
                    } | new Wrap(() => OnRechargeEnd(creature)),
                    new Wrap(Recharge) { Name = "Recharge Object" },
                    new Wrap(() => OnRechargeEnd(creature))
                    )) | new Wrap(() => OnRechargeEnd(creature));
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