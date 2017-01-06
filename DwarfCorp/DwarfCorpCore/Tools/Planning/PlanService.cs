// PlanService.cs
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
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{

    public abstract class GoalRegion
    {
        public abstract bool IsInGoalRegion(Voxel voxel);
        public abstract Voxel GetVoxel();
        public abstract float Heuristic(Voxel voxel);
        public abstract bool IsPossible();
    }

    public class VoxelGoalRegion : GoalRegion
    {
        public Voxel Voxel { get; set; }

        public override bool IsPossible()
        {
            return Voxel != null && !Voxel.Chunk.IsCompletelySurrounded(Voxel);
        }

        public override float Heuristic(Voxel voxel)
        {
            return (voxel.Position - Voxel.Position).LengthSquared();
        }

        public VoxelGoalRegion(Voxel voxel)
        {
            Voxel = voxel;
        }

        public override bool IsInGoalRegion(Voxel voxel)
        {
            return Voxel.Equals(voxel);
        }

        public override Voxel GetVoxel()
        {
            return Voxel;
        }
    }

    public class AdjacentVoxelGoalRegion2D : GoalRegion
    {
        public Voxel Voxel { get; set; }

        public override bool IsPossible()
        {
            return Voxel != null && !Voxel.Chunk.IsCompletelySurrounded(Voxel);
        }

        public override float Heuristic(Voxel voxel)
        {
            return (voxel.Position - Voxel.Position).LengthSquared();
        }

        public AdjacentVoxelGoalRegion2D(Voxel voxel)
        {
            Voxel = voxel;
        }

        public override bool IsInGoalRegion(Voxel voxel)
        {
            return Math.Abs(voxel.Position.X - Voxel.Position.X) <= 0.5f &&
                   Math.Abs(voxel.Position.Z - Voxel.Position.Z) <= 0.5f &&
                   Math.Abs(voxel.Position.Y - Voxel.Position.Y) < 0.001f;
        }

        public override Voxel GetVoxel()
        {
            return Voxel;
        }
    }


    // Tries to plan toward an edge of the world.
    public class EdgeGoalRegion : GoalRegion
    {
        public override bool IsInGoalRegion(Voxel voxel)
        {
            return Heuristic(voxel) < 2.0f;
        }

        public override Voxel GetVoxel()
        {
            return null;
        }

        public override float Heuristic(Voxel voxel)
        {
            BoundingBox worldBounds = voxel.Chunk.Manager.Bounds;
            Vector3 pos = voxel.Position;
            float value = MathFunctions.Dist2D(worldBounds, pos);
            return value;
        }

        public override bool IsPossible()
        {
            return true;
        }

    }

    public class SphereGoalRegion : GoalRegion
    {
        public Vector3 Position { get; set; }
        public Voxel Voxel { get; set; }
        private float r = 0.0f;
        public float Radius 
        {
            get
            {
                return r;
            }
            set
            {
                r = value;
                RadiusSquared = r*r;
            }
        } 

        private float RadiusSquared { get; set; }

        public SphereGoalRegion(Voxel voxel, float radius)
        {
            Radius = radius;
            Voxel = voxel;
            Position = voxel.Position;
        }

        public override float Heuristic(Voxel voxel)
        {
            return (voxel.Position - Voxel.Position).LengthSquared();
        }

        public override bool IsInGoalRegion(Voxel voxel)
        {
            return (voxel.Position - Position).LengthSquared() < RadiusSquared;
        }

        public override bool IsPossible()
        {
            return Voxel != null && !Voxel.Chunk.IsCompletelySurrounded(Voxel);
        }


        public override Voxel GetVoxel()
        {
            return Voxel;
        }
    }


    /// <summary>
    /// A request to plan from point A to point B
    /// </summary>
    public class AstarPlanRequest
    {
        public PlanSubscriber Subscriber;
        public CreatureAI Sender;
        public Voxel Start;
        public int MaxExpansions;
        public GoalRegion GoalRegion;
        public float HeuristicWeight = 1;
    }

    /// <summary>
    /// The result of a plan request (has a path on success)
    /// </summary>
    public class AStarPlanResponse
    {
        public bool Success;
        public List<Creature.MoveAction> Path;
    }

    /// <summary>
    /// A service call which plans from pointA to pointB voxels.
    /// </summary>
    public class PlanService : Service<AstarPlanRequest, AStarPlanResponse>
    {
        public override AStarPlanResponse HandleRequest(AstarPlanRequest req)
        {
            List<Creature.MoveAction> path = AStarPlanner.FindPath(req.Sender.Movement, req.Start, req.GoalRegion, PlayState.ChunkManager, req.MaxExpansions, req.HeuristicWeight);

            AStarPlanResponse res = new AStarPlanResponse
            {
                Path = path,
                Success = (path != null)
            };

            return res;
        }

    }

}