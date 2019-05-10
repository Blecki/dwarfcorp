using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PlaceVoxelAct : CreatureAct
    {
        public VoxelHandle Location;
        public ResourceAmount Resource;
        public String VoxelType;

        public PlaceVoxelAct(
            VoxelHandle Location,
            CreatureAI Agent,
            ResourceAmount Resource,
            String VoxelType) :
            base(Agent)
        {
            this.Location = Location;
            this.Resource = Resource;
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
            if (!Creature.Inventory.HasResource(Resource))
            {
                yield return Status.Fail;
            }

            foreach (var status in Creature.HitAndWait(1.0f, true, () => Location.Coordinate.ToVector3() + Vector3.One * 0.5f))
            {
                if (status == Status.Running)
                    yield return status;
            }

            var grabbed = Creature.Inventory.RemoveAndCreate(Resource, Inventory.RestockType.Any).FirstOrDefault();

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
                        .Select(c => new VoxelHandle(Agent.Chunks, c));

                    var closest = VoxelHandle.InvalidHandle;
                    float closestDist = float.MaxValue;
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

                TossMotion motion = new TossMotion(1.0f, 2.0f, grabbed.LocalTransform, Location.Coordinate.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
                grabbed.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                grabbed.AnimationQueue.Add(motion);

                var putType = Library.GetVoxelType(VoxelType);

                motion.OnComplete += () =>
                {
                    grabbed.Die();
                    PlaceVoxel(Location, putType, Creature.Manager.World);

                    Creature.Stats.NumBlocksPlaced++;
                    Creature.AI.AddXP(1);
                };

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