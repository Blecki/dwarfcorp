using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GoToTaggedObjectAct : CompoundCreatureAct
    {
        public string ObjectBlackboardName { get; set; }
        public bool Teleport { get; set; }
        public Vector3 TeleportOffset { get; set; }
        public bool CheckForOcclusion = true;

        public GoToTaggedObjectAct()
        {
            Name = "Go to tagged object";
            ObjectBlackboardName = "Tagged Object";
        }

        public GoToTaggedObjectAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Go to tagged object";
            ObjectBlackboardName = "Tagged Object";
        }


        public IEnumerable<Status> TeleportFunction()
        {
            GameComponent closestItem = Creature.AI.Blackboard.GetData<GameComponent>(ObjectBlackboardName);

            if (closestItem != null)
            {
                var location = TeleportOffset + closestItem.BoundingBox.Center();
                if (CheckForOcclusion)
                {
                    VoxelHandle voxAt = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(location));
                    bool gotLocation = false;
                    if (!voxAt.IsValid || !voxAt.IsEmpty)
                    {
                        // If we can't go to the preferred location, just try any free neighbor.
                        voxAt = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(closestItem.BoundingBox.Center()));
                        foreach (var neighbor in VoxelHelpers.EnumerateManhattanNeighbors2D(voxAt.Coordinate))
                        {
                            VoxelHandle newVox = new VoxelHandle(Agent.World.ChunkManager, neighbor);

                            if (newVox.IsValid && newVox.IsEmpty)
                            {
                                location = newVox.WorldPosition + new Vector3(0.5f, Agent.Physics.BoundingBox.Extents().Y, 0.5f);
                                gotLocation = true;
                                break;
                            }
                        }

                        // If there's no free neighbor, just teleport directly to the object.
                        if (!gotLocation)
                        {
                            location = closestItem.BoundingBox.Center();
                        }
                    }
                }
                TeleportAct act = new TeleportAct(Creature.AI) { Location = location };
                act.Initialize();
                foreach (Act.Status status in act.Run())
                {
                    yield return status;
                }

            }

            yield return Status.Fail;
        }

        public override void Initialize()
        {
            if (Teleport)
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectBlackboardName, Creature.AI) { PlanType = PlanAct.PlanType.Adjacent, MovingTarget = false } ,
                        new Wrap(TeleportFunction)
                        );
            }
            else
            {
                Tree =
                    new Sequence
                        (
                        new GoToEntityAct(ObjectBlackboardName, Creature.AI) { PlanType = PlanAct.PlanType.Adjacent, MovingTarget = false }
                        );
            }
            base.Initialize();
        }

 
    }
}
