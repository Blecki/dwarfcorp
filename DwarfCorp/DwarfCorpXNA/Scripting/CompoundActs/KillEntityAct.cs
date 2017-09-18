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
    /// <summary>
    /// A creature goes to an entity, and then hits it until the other entity is dead.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillEntityAct : CompoundCreatureAct
    {
        public Body Entity { get; set; }
        public KillEntityTask.KillType Mode { get; set; }
        public bool PathExists { get; set; }
        
        public KillEntityAct()
        {
            PathExists = false;
        }

        public bool Verify()
        {
            switch (Mode)
            {
                case KillEntityTask.KillType.Auto:
                {
                    return Entity != null && !Entity.IsDead;
                    break;
                }
                case KillEntityTask.KillType.Attack:
                {
                    return Entity != null && !Entity.IsDead && Creature.Faction.AttackDesignations.Contains(Entity);
                    break;
                }
                case KillEntityTask.KillType.Chop:
                {
                    return Entity != null && !Entity.IsDead && Creature.Faction.ChopDesignations.Contains(Entity);
                    break;
                }
            }
            return false;
        }

        public IEnumerable<Act.Status> OnAttackEnd(CreatureAI creature)
        {
            creature.Creature.OverrideCharacterMode = false;
            creature.Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }

        public KillEntityAct(Body entity, CreatureAI creature, KillEntityTask.KillType mode) :
            base(creature)
        {
            Mode = mode;
            Entity = entity;
            Name = "Kill Entity";
            PlanAct.PlanType planType = PlanAct.PlanType.Adjacent;
            float radius = 0.0f;
            if (creature.Creature.Attacks[0].Mode == Attack.AttackMode.Ranged)
            {
                planType = PlanAct.PlanType.Radius;
                radius = creature.Creature.Attacks[0].Range;
            }
            if (creature.Movement.IsSessile)
            {
                Tree =
                    new Domain(Verify,
                        new Sequence
                        (
                            new MeleeAct(Agent, entity)
                        ) | new Wrap(() => OnAttackEnd(creature))
                        );
            }
            else
            {
                Tree =
                    new Domain(Verify, new Sequence
                        (
                        new GoToEntityAct(entity, creature)
                        {
                            MovingTarget = mode != KillEntityTask.KillType.Chop,
                            PlanType = planType,
                            Radius = radius
                        } | new Wrap(() => OnAttackEnd(creature)),
                        new MeleeAct(Agent, entity)
                        ));

            }
        }
    }

}