// NecromancerAI.cs
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
    public class NecromancerAI : CreatureAI
    {
        public List<Skeleton> Skeletons { get; set; }
        public int MaxSkeletons { get; set; }
        public Timer SummonTimer { get; set; }
        public Timer AttackTimer { get; set; }
        public float AttackRange { get; set; }

        public NecromancerAI()
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 5;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }

        public NecromancerAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 5;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }
        
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(new Wrap(SummonSkeletons))
            {
                Priority = Task.PriorityType.High
            };
        }

        public void SummonSkeleton()
        {
            Vector3 pos = Position + MathFunctions.RandVector3Box(-1.0f, 1.0f, 0.0f, 0.0f, -1.0f, 1.0f);
            Skeleton skeleton = EntityFactory.GenerateSkeleton(pos, Manager, GameState.Game.Content,
                GameState.Game.GraphicsDevice, Chunks, Manager.World.Camera, Faction, Manager.World.PlanService,
                this.Creature.Allies).GetChildrenOfType<Skeleton>().FirstOrDefault();

            Skeletons.Add(skeleton);
            Matrix animatePosition = skeleton.Sprite.LocalTransform;
            animatePosition.Translation = animatePosition.Translation - new Vector3(0, 1, 0);
            skeleton.Sprite.AnimationQueue.Add(new EaseMotion(1.0f, animatePosition, skeleton.Sprite.LocalTransform.Translation));
            Manager.World.ParticleManager.Trigger("green_flame", pos, Color.White, 10);
            SoundManager.PlaySound(ContentPaths.Audio.tinkle, pos, true);
        }

        public void GatherSkeletons()
        {
            foreach (Skeleton skeleton in Skeletons)
            {
                Vector3 offset = Position - skeleton.AI.Position;
                float dist = (offset).Length();
                if (dist > 4 && skeleton.AI.Tasks.Count <= 1)
                {
                    Task goToTask = new ActWrapperTask(new GoToEntityAct(Physics, skeleton.AI))
                    {
                        Priority = Task.PriorityType.High
                    };
                    if (!skeleton.AI.Tasks.Contains(goToTask))
                    {
                        skeleton.AI.Tasks.Add(goToTask);
                    }
                }
            }
        }

        public void OrderSkeletonsToAttack()
        {
            List<CreatureAI> enemies = (from faction in Creature.Manager.Factions.Factions
                                        where Manager.World.ComponentManager.Diplomacy.GetPolitics(Creature.Faction, faction.Value).GetCurrentRelationship() == Relationship.Hateful 
                                        from minion in faction.Value.Minions 
                                        let dist = (minion.Position - Creature.AI.Position).Length() 
                                        where dist < AttackRange 
                                        select minion).ToList();

            List<Task> attackTasks = enemies.Select(enemy => new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto)).Cast<Task>().ToList();
            List<CreatureAI> skeletonAis = Skeletons.Select(skeleton => skeleton.AI).ToList();
            if (attackTasks.Count > 0)
            {
                TaskManager.AssignTasks(attackTasks, skeletonAis);
            }
        }

        public IEnumerable<Act.Status> SummonSkeletons()
        {
            while (true)
            {
                Skeletons.RemoveAll(skeleton => skeleton.IsDead);
                if (SummonTimer.HasTriggered && Skeletons.Count < MaxSkeletons)
                {
                    SummonTimer.Reset(SummonTimer.TargetTimeSeconds);
                    for (int i = Skeletons.Count; i < MaxSkeletons; i+=2)
                    {
                        SummonSkeleton();
                    }
                    yield return Act.Status.Success;
                }
                else if (SummonTimer.HasTriggered)
                {
                    yield return Act.Status.Success;
                }
                SummonTimer.Update(DwarfTime.LastTime);

                if (WanderTimer.HasTriggered)
                {
                    Physics.ApplyForce(MathFunctions.RandVector3Box(-5f, 5f, 0.01f, 0.01f, -5f, 5f), 1f);
                    GatherSkeletons();
                }
                WanderTimer.Update(DwarfTime.LastTime);

                if (AttackTimer.HasTriggered)
                {
                    OrderSkeletonsToAttack();
                }
                AttackTimer.Update(DwarfTime.LastTime);
                yield return Act.Status.Running;
            }
        }
    }
}
