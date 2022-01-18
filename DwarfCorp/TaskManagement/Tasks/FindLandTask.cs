using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// If the creature is in liquid, causes it to find the nearest land and go there.
    /// </summary>
    /// <seealso cref="Task" />
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class FindLandTask : Task
    {
        public FindLandTask()
        {
            Name = "Find Land";
            Priority = TaskPriority.High;
        }

        public VoxelHandle FindLand(
            ChunkManager Data,
            GlobalVoxelCoordinate Start,
            int Radius)
        { 
            GlobalVoxelCoordinate landFound;
            if (VoxelHelpers.BreadthFirstSearch(Data, Start, Radius,
                coord =>
                {
                    var v = new VoxelHandle(Data, coord);
                    if (!v.IsValid || !v.IsEmpty || LiquidCellHelpers.AnyLiquidInVoxel(v)) return false;
                    var below = new VoxelHandle(Data,
                        new GlobalVoxelCoordinate(coord.X, coord.Y - 1, coord.Z));
                    return below.IsValid && !below.IsEmpty;
                },
                out landFound))
                return new VoxelHandle(Data, landFound);
            return VoxelHandle.InvalidHandle;
        }
         
        public IEnumerable<VoxelHandle> FindLandNonBlocking(
            ChunkManager Data,
            GlobalVoxelCoordinate Start,
            int Radius)
        {
            int chunkSize = 64;
            int k = 0;
            foreach (var coord in VoxelHelpers.BreadthFirstSearchNonBlocking(Data, Start, Radius,
                c =>
                {
                    var v = new VoxelHandle(Data, c);
                    if (!v.IsValid || !v.IsEmpty || LiquidCellHelpers.AnyLiquidInVoxel(v)) return false;
                    var below = new VoxelHandle(Data,
                        new GlobalVoxelCoordinate(c.X, c.Y - 1, c.Z));
                    return below.IsValid && !below.IsEmpty;
                })) 
            {
                var v = new VoxelHandle(Data, coord);
                if (v.IsValid || k % chunkSize == 0)
                {
                    yield return v;
                }
                k++;
            }
        }

        public IEnumerable<Act.Status> SwimUp(Creature creature)
        {
            Timer timer = new Timer(10.0f, false, Timer.TimerMode.Game);

            while (!timer.HasTriggered)
            {
                timer.Update(creature.AI.FrameDeltaTime);

                creature.Physics.ApplyForce(Vector3.Up * 25, (float)creature.AI.FrameDeltaTime.ElapsedGameTime.TotalSeconds);

                if (!creature.Physics.IsInLiquid)
                {
                    yield return Act.Status.Success;
                    yield break;
                }
                yield return Act.Status.Running;
            }
            yield return Act.Status.Fail;
        }

        public IEnumerable<Act.Status> FindLandEnum(Creature creature)
        {
            foreach (var handle in FindLandNonBlocking(creature.World.ChunkManager,
                creature.Physics.CurrentVoxel.Coordinate, 100))
            {
               if (handle.IsValid)
                {
                    creature.AI.Blackboard.SetData<VoxelHandle>("Land", handle);
                    yield return Act.Status.Success;
                    yield break;
                }
                yield return Act.Status.Running;
            }
            creature.AI.SetTaskFailureReason("Could not find land.");
            yield return Act.Status.Fail;
        }

        public override MaybeNull<Act> CreateScript(Creature creature)
        {
            var above = VoxelHelpers.GetVoxelAbove(creature.Physics.CurrentVoxel);
            if ((above.IsValid && LiquidCellHelpers.AnyLiquidInVoxel(above)) || creature.AI.Movement.CanFly)
            {
                return new Wrap(() => SwimUp(creature)) { Name = "Swim up"};
            }

            Act fallback = null;

            if (creature.World.EnumerateZones().Count() == 0)
                fallback = new LongWanderAct(creature.AI) { PathLength = 20, Radius = 30, Is2D = true, Name = "Randomly wander." };
            else
                fallback = new GoToZoneAct(creature.AI, Datastructures.SelectRandom(creature.World.EnumerateZones()));

            return new Select(new Sequence(new Wrap(() => FindLandEnum(creature)) { Name = "Search for land." },
                                           new GoToNamedVoxelAct("Land", PlanAct.PlanType.Into, creature.AI)),
                              fallback)
            { Name = "Find Land" };
        }

        public override float  ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Physics.IsInLiquid ? Feasibility.Feasible : Feasibility.Infeasible;
        }
    }

}
