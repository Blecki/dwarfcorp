// GoToEntityAct.cs
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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds the voxel below a given entity, and goes to it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GoToEntityAct : CompoundCreatureAct
    {
        public Body Entity { get { return Agent.Blackboard.GetData<Body>(EntityName);  } set {Agent.Blackboard.SetData(EntityName, value);} }
        public bool MovingTarget { get; set; }
        public string EntityName { get; set; }
        public PlanAct.PlanType PlanType { get; set; }
        public float Radius { get; set; }

        public GoToEntityAct()
        {
            PlanType = PlanAct.PlanType.Adjacent;
            Radius = 3.0f;
        }

        public bool EntityIsInHands()
        {
            return Entity == Agent.Hands.GetFirstGrab();
        }

        public Condition InHands()
        {
            return new Condition(EntityIsInHands);
        }

        public GoToEntityAct(string entity, CreatureAI creature) :
            base(creature)
        {
            Name = "Go to entity " + entity;
            EntityName = entity;
            MovingTarget = true;
        }

        public GoToEntityAct(Body entity, CreatureAI creature) :
            base(creature)
        {
            Name = "Go to entity";
            EntityName = "TargetEntity";
            Entity = entity;
            MovingTarget = true;
        }

        public IEnumerable<Status> CollidesWithTarget()
        {
            while (true)
            {
                if (Entity.BoundingBox.Intersects(Creature.Physics.BoundingBox))
                {
                    yield return Status.Success;
                    yield break;
                }

                yield return Status.Running;
            }
        }

        public IEnumerable<Status> TargetMoved(string pathName)
        {
            var path = Agent.Blackboard.GetData<List<VoxelHandle>>(pathName);
            Body entity = Agent.Blackboard.GetData<Body>(EntityName);
            if (path == null || entity == null)
            {
                yield return Status.Success;
                yield break;
            }
            
            while (true)
            {
                if (path.Count == 0)
                {
                    yield return Status.Success;
                    yield break;
                }

                var last = path.Last();

                if ((last.WorldPosition - entity.LocalTransform.Translation).Length() > Radius * 2)
                {
                    yield return Status.Fail;
                    yield break;
                }

                yield return Status.Running;
            }
        }

        public IEnumerable<Status> TrackMovingTarget()
        {
            int maxFailures = 1;
            int currentFailures = 0;
            while (true)
            {
                Creature.AI.Blackboard.Erase("EntityVoxel");
                Act.Status status = SetTargetVoxelFromEntityAct.SetTarget("EntityVoxel", EntityName, Creature);
                Body entity = Agent.Blackboard.GetData<Body>(EntityName);

                if (entity == null || entity.IsDead)
                {
                    yield return Status.Success;
                    yield break;
                }

                if (status != Status.Success)
                {
                    yield return Act.Status.Running;
                }
                List<MoveAction> existingPath =
                    Creature.AI.Blackboard.GetData<List<MoveAction>>("PathToEntity");

                Creature.AI.Blackboard.Erase("PathToEntity");

                PlanWithGreedyFallbackAct planAct = new PlanWithGreedyFallbackAct() { Agent = Creature.AI,
                    PathName = "PathToEntity", VoxelName = "EntityVoxel", PlanType = PlanType, Radius = Radius, MaxTimeouts = 1 };
                planAct.Initialize();

                bool planSucceeded = false;
                while (true)
                {
                    Act.Status planStatus = planAct.Tick();

                    if (planStatus == Status.Fail)
                    {
                        yield return Act.Status.Running;
                        break;
                    }

                    else if (planStatus == Status.Running)
                    {
                        yield return Act.Status.Running;
                    }

                    else if (planStatus == Status.Success)
                    {
                        planSucceeded = true;
                        break;
                    }

                }
                
                if (!planSucceeded)
                {
                    yield return Act.Status.Fail;
                }

                FollowPathAct followPath = new FollowPathAct(Creature.AI, "PathToEntity")
                {
                    BlendEnd = true,
                    BlendStart = existingPath == null
                };
                followPath.Initialize();

                while (true)
                {
                    if (PlanType == PlanAct.PlanType.Radius && (Creature.Physics.Position - entity.Position).Length() < Radius)
                    {
                        yield return Act.Status.Success;
                    }

                    Act.Status pathStatus = followPath.Tick();

                    if (pathStatus == Status.Fail)
                    {
                        break;
                    }

                    else if (pathStatus == Status.Running)
                    {
                        yield return Act.Status.Running;

                        List<MoveAction> path = Agent.Blackboard.GetData<List<MoveAction>>("PathToEntity");
                        if (path == null || path.Count == 0)
                        {
                            yield return Act.Status.Fail;
                            yield break;
                        }

                        bool targetMoved = (path.Last().DestinationVoxel.WorldPosition - entity.LocalTransform.Translation).Length() > Math.Max(Radius, 2) * 2;

                        if (MovingTarget && (path.Count > 0 && targetMoved))
                        {
                            break;
                        }

                        if (MovingTarget && (Creature.Physics.Position - entity.Position).Length() < 2)
                        {
                            yield return Status.Success;
                            yield break;
                        }

                        continue;
                    }

                    else if (pathStatus == Status.Success)
                    {
                        yield return Act.Status.Success;
                        yield break;
                    }

                }
                
                yield return Act.Status.Running;
            }
        }


        public override void Initialize()
        {
            /*
            Creature.AI.Blackboard.Erase("PathToEntity");
            Creature.AI.Blackboard.Erase("EntityVoxel");
            Tree = new Sequence(
                new Wrap(() => Creature.ClearBlackboardData("PathToEntity")), 
                new Wrap(() => Creature.ClearBlackboardData("EntityVoxel")),
                InHands() |
                 new Sequence(
                    new ForLoop(
                        new Sequence( 
                            new SetTargetVoxelFromEntityAct(Agent, EntityName, "EntityVoxel"),
                            new PlanAct(Agent, "PathToEntity", "EntityVoxel", PlanAct.PlanType.Adjacent),
                            new Parallel( new FollowPathAct(Agent, "PathToEntity"), 
                                          new Wrap(() => TargetMoved("PathToEntity")), 
                                          new Wrap(CollidesWithTarget)) { ReturnOnAllSucces = false }
                                     ), 
                                5, true),
                    new StopAct(Agent)));
             */
            Tree = new Sequence(
                new Wrap(TrackMovingTarget),
                new StopAct(Agent)
                );

        Tree.Initialize();
            base.Initialize();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}