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

        public KillEntityTask()
        {
            MaxAssignable = 3;
        }

        public KillEntityTask(Body entity, KillType type)
        {
            MaxAssignable = 3;
            Mode = type;
            Name = "Kill Entity: " + entity.Name + " " + entity.GlobalID;
            EntityToKill = entity;
            Priority = PriorityType.Urgent;
            AutoRetry = true;
            if (type == KillType.Attack || type == KillType.Auto)
            {
                Category = TaskCategory.Attack;
            }
            else if (type == KillType.Chop)
            {
                Category = TaskCategory.Chop;
            }
        }

        public override Task Clone()
        {
            return new KillEntityTask(EntityToKill, Mode) { Priority = this.Priority, AutoRetry = this.AutoRetry };
        }

        public override Act CreateScript(Creature creature)
        {
            var otherCreature = EntityToKill.GetRoot().GetComponent<Creature>();
            if (otherCreature != null && !creature.AI.FightOrFlight(otherCreature.AI))
            {
                Name = "Flee Entity: " + EntityToKill.Name + " " + EntityToKill.GlobalID;
                IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.Exclaim, creature.AI.Position, 1.0f, 1.0f, Vector2.UnitY * -32);
                return new FleeEntityAct(creature.AI) {Entity = EntityToKill, PathLength = 5};
            }
            return new KillEntityAct(EntityToKill, creature.AI, Mode);
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

            switch (Mode)
            {
                case KillType.Attack:
                    return !agent.Faction.Designations.IsDesignation(EntityToKill, DesignationType.Attack);
                case KillType.Chop:
                    return !agent.Faction.Designations.IsDesignation(EntityToKill, DesignationType.Chop);
                case KillType.Auto:
                    return false;
            }

            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {

            if (EntityToKill == null || EntityToKill.IsDead)
            {
                return Feasibility.Infeasible;
            }
            else
            {
                var ai = EntityToKill.EnumerateAll().OfType<Creature>().FirstOrDefault();

                switch (Mode)
                {
                    case KillType.Attack:
                        if (!agent.Stats.CurrentClass.HasAction(Task.TaskCategory.Attack))
                            return Feasibility.Infeasible;
                        return agent.Faction.Designations.IsDesignation(EntityToKill, DesignationType.Attack) ? Feasibility.Feasible : Feasibility.Infeasible;
                    case KillType.Chop:
                        if (!agent.Stats.CurrentClass.HasAction(Task.TaskCategory.Chop))
                            return Feasibility.Infeasible;
                        return agent.Faction.Designations.IsDesignation(EntityToKill, DesignationType.Chop) ? Feasibility.Feasible : Feasibility.Infeasible;
                    case KillType.Auto:
                        return Feasibility.Feasible;
                }

                var target = new VoxelHandle(agent.World.ChunkManager.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(EntityToKill.Position));
                // Todo: Find out if it is calculating the path twice to make PathExists work.
                if (!target.IsValid)
                {
                    return Feasibility.Infeasible;
                }


                if(ai == null)
                {
                    return Feasibility.Infeasible;
                }
                Relationship relation = 
                    agent.World.Diplomacy.GetPolitics(ai.Faction, agent.Faction).GetCurrentRelationship();
                return (relation == Relationship.Hateful || relation == Relationship.Indifferent) ? Feasibility.Feasible : Feasibility.Infeasible;
            }
        }
    }

}
