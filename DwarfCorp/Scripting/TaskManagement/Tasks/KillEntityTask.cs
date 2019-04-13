// KillEntityTask.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    // Todo: Seperate tasks for chop/attack

    /// <summary>
    /// Tells a creature that it should kill an entity.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class KillEntityTask : Task
    {
        public enum KillType
        {
            Attack,
            Auto
        }
        public Body EntityToKill = null;
        public KillType Mode { get; set; }

        public KillEntityTask()
        {
            MaxAssignable = 3;
            BoredomIncrease = GameSettings.Default.Boredom_ExcitingTask;
        }

        public KillEntityTask(Body entity, KillType type)
        {
            MaxAssignable = 3;
            Mode = type;
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = PriorityType.Urgent;
            AutoRetry = true;
            Category = TaskCategory.Attack;
            BoredomIncrease = GameSettings.Default.Boredom_ExcitingTask;
            if (type == KillType.Auto)
            {
                ReassignOnDeath = false;
            }
        }


        public override Act CreateScript(Creature creature)
        {
            if (creature.IsDead || creature.AI.IsDead)
                return null;

            var otherCreature = EntityToKill.GetRoot().GetComponent<Creature>();

            if (otherCreature != null && (!otherCreature.IsDead && otherCreature.AI != null))
            {

                var otherKill = new KillEntityTask(creature.Physics, KillType.Auto)
                {
                    AutoRetry = true,
                    ReassignOnDeath = false
                };

                if (!otherCreature.AI.HasTaskWithName(otherKill))
                {
                    otherCreature.AI.AssignTask(otherKill);
                }

                if (otherCreature != null && !creature.AI.FightOrFlight(otherCreature.AI))
                {
                    Name = "Flee Entity: " + EntityToKill.Name + " " + EntityToKill.GlobalID;
                    ReassignOnDeath = false;
                    IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.Exclaim, creature.AI.Position, 1.0f, 1.0f, Vector2.UnitY * -32);
                    return new FleeEntityAct(creature.AI) { Entity = EntityToKill, PathLength = 20 };
                }
            }
            float radius = this.Mode == KillType.Auto ? 20.0f : 0.0f;
            return new KillEntityAct(EntityToKill, creature.AI) { RadiusDomain = radius };
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            if (agent == null || EntityToKill == null)
            {
                return 10000;
            }

            else return (agent.AI.Position - EntityToKill.LocalTransform.Translation).LengthSquared() * 0.01f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToKill != null && !EntityToKill.IsDead;
        }

        public override bool ShouldDelete(Creature agent)
        {
            if (EntityToKill == null || EntityToKill.IsDead || (EntityToKill.Position - agent.AI.Position).Length() > 100)
            {
                return true;
            }

            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent == null || agent.IsDead || EntityToKill == null || EntityToKill.IsDead)
                return Feasibility.Infeasible;
            else
            {
                var ai = EntityToKill.EnumerateAll().OfType<Creature>().FirstOrDefault();

                if (Mode == KillType.Attack && !agent.Stats.IsTaskAllowed(TaskCategory.Attack))
                    return Feasibility.Infeasible;
                else if (Mode == KillType.Auto && (agent.AI.Position - EntityToKill.Position).Length() > 20)
                    return Feasibility.Infeasible;

                return Feasibility.Feasible;
            }
        }

        public override bool IsComplete(Faction faction)
        {
            return EntityToKill == null || EntityToKill.IsDead;
        }

        public override void OnEnqueued(Faction Faction)
        {
            Faction.Designations.AddEntityDesignation(EntityToKill, DesignationType.Attack, null, this);
        }

        public override void OnDequeued(Faction Faction)
        {
            Faction.Designations.RemoveEntityDesignation(EntityToKill, DesignationType.Attack);
        }
    }

}
