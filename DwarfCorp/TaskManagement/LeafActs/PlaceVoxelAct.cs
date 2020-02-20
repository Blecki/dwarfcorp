using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class PlaceVoxelAct : CreatureAct
    {
        public VoxelHandle Location;
        public String VoxelType;
        public String ResourceBlackboardName;

        public PlaceVoxelAct(
            VoxelHandle Location,
            CreatureAI Agent,
            String ResourceBlackboardName,
            String VoxelType) :
            base(Agent)
        {
            this.Location = Location;
            this.ResourceBlackboardName = ResourceBlackboardName;
            this.VoxelType = VoxelType;

            Name = "Build DestinationVoxel " + Location.ToString();
        }

        public override void OnCanceled()
        {
            Creature.Physics.Active = true;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            foreach (var res in Agent.Blackboard.GetData<List<Resource>>(ResourceBlackboardName))
                if (!Creature.Inventory.Contains(res))
                    yield return Status.Fail;

            foreach (var status in Creature.HitAndWait(1.0f, true, () => Location.Coordinate.ToVector3() + Vector3.One * 0.5f))
            {
                if (status == Status.Running)
                    yield return status;
            }

            foreach (var res in Agent.Blackboard.GetData<List<Resource>>(ResourceBlackboardName))
            { 
                var grabbed = Creature.Inventory.RemoveAndCreate(res, Inventory.RestockType.Any);

                if (grabbed == null)
                {
                    yield return Status.Fail;
                    yield break;
                }
                else
                {
                    // If the creature intersects the box, find a voxel adjacent to it that is free, and jump there to avoid getting crushed.
                    if (Creature.Physics.BoundingBox.Intersects(Location.GetBoundingBox()))
                    {
                        var neighbors = VoxelHelpers.EnumerateAllNeighbors(Location.Coordinate)
                            .Select(c => new VoxelHandle(Agent.World.ChunkManager, c));

                        var closest = VoxelHandle.InvalidHandle;
                        var closestDist = float.MaxValue;
                        foreach (var voxel in neighbors)
                        {
                            if (!voxel.IsValid) continue;

                            float dist = (voxel.WorldPosition - Creature.Physics.Position).LengthSquared();
                            if (dist < closestDist && voxel.IsEmpty)
                            {
                                closestDist = dist;
                                closest = voxel;
                            }
                        }

                        if (closest.IsValid)
                        {
                            TossMotion teleport = new TossMotion(0.5f, 1.0f, Creature.Physics.GlobalTransform, closest.WorldPosition + Vector3.One * 0.5f);
                            Creature.Physics.AnimationQueue.Add(teleport);
                        }
                    }

                    // Todo: Shitbox - what happens if the player saves while this animation is in progress?? How is the OnComplete restored?
                    var motion = new TossMotion(1.0f, 2.0f, grabbed.LocalTransform, Location.Coordinate.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
                    if (grabbed.GetRoot().GetComponent<Physics>().HasValue(out var grabbedPhysics))
                        grabbedPhysics.CollideMode = Physics.CollisionMode.None;
                    grabbed.AnimationQueue.Add(motion);

                    motion.OnComplete += () => grabbed.Die();
                }

                if (Library.GetVoxelType(VoxelType).HasValue(out var vType))                
                    PlaceVoxel(Location, vType, Creature.Manager.World);

                Creature.Stats.NumBlocksPlaced++;
                Creature.AI.AddXP(1);
                ActHelper.ApplyWearToTool(Creature.AI, GameSettings.Current.Wear_Craft);

                yield return Status.Success;
                yield break;
            }
        }

        private void PlaceVoxel(VoxelHandle Vox, VoxelType Type, WorldManager World)
        {
            Vox.IsPlayerBuilt = true;
            Vox.Type = Type;
            Vox.QuickSetLiquid(LiquidType.None, 0);

            for (int i = 0; i < 4; i++)
                World.ParticleManager.Trigger("puff", MathFunctions.RandVector3Box(Vox.GetBoundingBox().Expand(0.25f)), Color.White, 5);

            // Todo: Should this be handled by the chunk manager while processing voxel update events?
            foreach (var phys in World.EnumerateIntersectingObjects(Vox.GetBoundingBox(), CollisionType.Dynamic).OfType<Physics>())
            {
                phys.ApplyForce((phys.GlobalTransform.Translation - (Vox.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))) * 100, 0.01f);
                BoundingBox box = Vox.GetBoundingBox();
                Physics.Contact contact = new Physics.Contact();
                Physics.TestStaticAABBAABB(box, phys.GetBoundingBox(), ref contact);

                if (!contact.IsIntersecting)
                {
                    continue;
                }

                Vector3 diff = contact.NEnter * contact.Penetration;
                Matrix m = phys.LocalTransform;
                m.Translation += diff;
                phys.LocalTransform = m;
            }
        }
    }

}