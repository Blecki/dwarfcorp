using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
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
    }

    public class VoxelGoalRegion : GoalRegion
    {
        public Voxel Voxel { get; set; }


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

        public override bool IsInGoalRegion(Voxel voxel)
        {
            return (voxel.Position - Position).LengthSquared() < RadiusSquared;
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
            List<Creature.MoveAction> path = AStarPlanner.FindPath(req.Start, req.GoalRegion, PlayState.ChunkManager, req.MaxExpansions);

            AStarPlanResponse res = new AStarPlanResponse
            {
                Path = path,
                Success = (path != null)
            };

            return res;
        }

    }

}