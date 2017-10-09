// PlaceVoxelAct.cs
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
    /// A creature uses the item currently in its hands to construct a voxel.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PlaceVoxelAct : CreatureAct
    {
        public GlobalVoxelCoordinate Location;
        public ResourceAmount Resource;

        public PlaceVoxelAct(
            GlobalVoxelCoordinate Location,
            CreatureAI Agent,
            ResourceAmount Resource) :
            base(Agent)
        {
            this.Location = Location;
            this.Resource = Resource;

            Name = "Build DestinationVoxel " + Location.ToString();
        }

        public override IEnumerable<Status> Run()
        {
            if (!Creature.Faction.IsPutDesignation(Location))
            {
                yield return Status.Success;
                yield break;
            }

            if (!Creature.Inventory.HasResource(Resource))
            {
                yield return Status.Fail;
            }

            foreach (var status in Creature.HitAndWait(1.0f, true, () => Location.ToVector3() + Vector3.One * 0.5f))
            {
                if (!Creature.Faction.IsPutDesignation(Location))
                {
                    yield return Status.Success;
                    yield break;
                }
                if (status == Status.Running)
                {
                    yield return status;
                }
            }

            var grabbed = Creature.Inventory.RemoveAndCreate(Resource).FirstOrDefault();

            if(grabbed == null)
            {
                yield return Status.Fail;
                yield break;
            }
            else
            {
                if(Creature.Faction.IsPutDesignation(Location))
                {
                    // If the creature intersects the box, find a voxel adjacent to it that is free, and jump there to avoid getting crushed.
                    if (Creature.Physics.BoundingBox.Intersects(new BoundingBox(
                        Location.ToVector3(), Location.ToVector3() + Vector3.One)))
                    {
                        var neighbors = VoxelHelpers.EnumerateAllNeighbors(Location)
                            .Select(c => new VoxelHandle(Agent.Chunks.ChunkData, c));

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
                    TossMotion motion = new TossMotion(1.0f, 2.0f, grabbed.LocalTransform, Location.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
                    motion.OnComplete += grabbed.Die;
                    grabbed.GetRoot().GetComponent<Physics>().CollideMode = Physics.CollisionMode.None;
                    grabbed.AnimationQueue.Add(motion);

                    var put = Creature.Faction.GetPutDesignation(Location);
                    PlaceVoxel(put.Voxel, put.Type, Creature.Manager.World);
                    
                    Creature.Faction.RemovePutDesignation(put.Voxel);
                    Creature.Stats.NumBlocksPlaced++;
                    Creature.AI.AddXP(1);
                    yield return Status.Success;
                }
                else
                {
                    Creature.Inventory.Pickup(grabbed, Inventory.RestockType.RestockResource);
                    grabbed.Die();
                    
                    yield return Status.Success;
                }
            }
        }

        private void PlaceVoxel(VoxelHandle Vox, VoxelType Type, WorldManager World)
        {
            Vox.Type = Type;
            Vox.WaterCell = new WaterCell();
            Vox.Health = Type.StartingHealth;
            World.ParticleManager.Trigger("puff", Vox.WorldPosition, Color.White, 20);

            foreach (Physics phys in World.CollisionManager.EnumerateIntersectingObjects(Vox.GetBoundingBox(), CollisionManager.CollisionType.Dynamic).OfType<Physics>())
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