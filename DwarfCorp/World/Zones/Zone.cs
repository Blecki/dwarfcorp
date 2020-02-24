using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Zone
    {
        public string ID = "";
        public List<VoxelHandle> Voxels = new List<VoxelHandle>();
        public List<GameComponent> ZoneBodies = new List<GameComponent>();

        [JsonProperty] private String TypeName;
        [JsonIgnore] private ZoneType _cachedType;
        [JsonIgnore] public ZoneType Type
        {
            get
            {
                if (_cachedType == null && Library.GetZoneType(TypeName).HasValue(out var type))
                    _cachedType = type;
                return _cachedType;
            }
        }

        public bool IsBuilt;
        public virtual String GetDescriptionString() { return Library.GetString("generic-room-description"); }

        [JsonIgnore] public Gui.Widget GuiTag;
        [JsonIgnore] public WorldManager World;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = (WorldManager)ctx.Context;
            foreach (var body in ZoneBodies)
            {
                GameComponent body1 = body;
                body.OnDestroyed += () => body_onDestroyed(body1);
            }
        }

        public Zone(String TypeName, WorldManager World)
        {
            this.World = World;
            this.TypeName = TypeName;

            ID = World.PersistentData.NextRoomID + ". " + TypeName;
            ++World.PersistentData.NextRoomID;
        }

        public Zone()
        {

        }

        public GameComponent GetNearestBody(Vector3 location)
        {
            GameComponent toReturn = null;
            var nearestDistance = float.MaxValue;

            foreach (var body in ZoneBodies)
            {
                var dist = (location - body.Position).LengthSquared();
                if (dist < nearestDistance)
                {
                    toReturn = body;
                    nearestDistance = dist;
                }
            }

            return toReturn;
        }

        public void SetTint(Color color)
        {
            foreach (var body in ZoneBodies)
                foreach (var sprite in body.EnumerateAll().OfType<Tinter>())
                    sprite.VertexColorTint = color;
        }

        public GameComponent GetNearestBodyWithTag(Vector3 location, string tag, bool filterReserved)
        {
            GameComponent toReturn = null;
            var nearestDistance = float.MaxValue;

            foreach (GameComponent body in ZoneBodies)
            {
                if (!body.Tags.Contains(tag)) continue;
                if (filterReserved && (body.IsReserved || body.ReservedFor != null)) continue;

                float dist = (location - body.GlobalTransform.Translation).LengthSquared();
                if (dist < nearestDistance)
                {
                    toReturn = body;
                    nearestDistance = dist;
                }
            }

            return toReturn;
        }

        public void AddBody(GameComponent body)
        {
            ZoneBodies.Add(body);
            body.OnDestroyed += () => body_onDestroyed(body);
        }

        public void body_onDestroyed(GameComponent body)
        {
            ZoneBodies.Remove(body);
        }

        public virtual void Destroy()
        {
            List<GameComponent> toKill = new List<GameComponent>();
            toKill.AddRange(ZoneBodies);
            foreach (GameComponent body in toKill)
                body.Die();

            foreach (var voxel in Voxels)
            {
                var v = new VoxelHandle(voxel.Chunk.Manager, voxel.Coordinate);
                v.DecalType = 0;
            }

            var voxelsToKill = new List<VoxelHandle>();
            voxelsToKill.AddRange(Voxels);
            foreach (var voxel in voxelsToKill)
            {
                World.ParticleManager.Trigger("dirt_particle", voxel.WorldPosition + Vector3.Up, Color.White, 1);
                RemoveVoxel(voxel);
            }

            ZoneBodies.Clear();
            Voxels.Clear();
        }

        
        public bool ContainsVoxel(VoxelHandle voxel)
        {
            return Voxels.Any(store => store == voxel);
        }

        public virtual bool RemoveVoxel(VoxelHandle voxel)
        {
            bool removed = false;
            for (int i = 0; i < Voxels.Count; i++)
            {
                var toRemove = Voxels[i];
                if (toRemove != voxel)
                    continue;
                if (!toRemove.IsValid)
                    return true;

                Voxels.Remove(toRemove);
                toRemove.IsPlayerBuilt = false;
                removed = true;
                break;
            }
            return removed;
        }

        public virtual void AddVoxel(VoxelHandle Voxel)
        {
            if(ContainsVoxel(Voxel))
                return;
            Voxel.IsPlayerBuilt = true;
            Voxels.Add(Voxel);

            //if (Library.GetVoxelType(Type.FloorType).HasValue(out VoxelType floor))
            //    Voxel.Type = floor;
        }

        public VoxelHandle GetNearestVoxel(Vector3 position)
        {
            VoxelHandle closest = VoxelHandle.InvalidHandle;
            Vector3 halfSize = new Vector3(0.5f, 0.5f, 0.5f);
            double closestDist = double.MaxValue;

            foreach (var v in Voxels)
            {
                double d = (v.WorldPosition - position + halfSize).LengthSquared();

                if(d < closestDist)
                {
                    closestDist = d;
                    closest = v;
                }
            }

            return closest;
        }

        public BoundingBox GetBoundingBox()
        {
            var minX = Int32.MaxValue;
            var minY = Int32.MaxValue;
            var minZ = Int32.MaxValue;
            var maxX = Int32.MinValue;
            var maxY = Int32.MinValue;
            var maxZ = Int32.MinValue;

            for (var i = 0; i < Voxels.Count; ++i)
            {
                var v = Voxels[i].Coordinate;
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }

            return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX + 1, maxY + 1, maxZ + 1));
        }
                
        public virtual void OnBuilt()
        {
            if (Type.Outline)
            {
                List<BoundingBox> boxes = Voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
                BoundingBox box = MathFunctions.GetBoundingBox(boxes);
                foreach (var voxel in Voxels)
                {
                    var loc = voxel;
                    if (voxel.Coordinate.X == box.Min.X && voxel.Coordinate.Z == box.Min.Z)
                        loc.DecalType = Library.GetDecalType("zone-nw").ID;
                    else if (voxel.Coordinate.X == box.Min.X && voxel.Coordinate.Z == box.Max.Z - 1)
                        loc.DecalType = Library.GetDecalType("zone-sw").ID;
                    else if (voxel.Coordinate.X == box.Max.X - 1 && voxel.Coordinate.Z == box.Min.Z)
                        loc.DecalType = Library.GetDecalType("zone-ne").ID;
                    else if (voxel.Coordinate.X == box.Max.X - 1 && voxel.Coordinate.Z == box.Max.Z - 1)
                        loc.DecalType = Library.GetDecalType("zone-se").ID;
                    else if (voxel.Coordinate.X == box.Min.X)
                        loc.DecalType = Library.GetDecalType("zone-w").ID;
                    else if (voxel.Coordinate.X == box.Max.X - 1)
                        loc.DecalType = Library.GetDecalType("zone-e").ID;
                    else if (voxel.Coordinate.Z == box.Min.Z)
                        loc.DecalType = Library.GetDecalType("zone-n").ID;
                    else if (voxel.Coordinate.Z == box.Max.Z - 1)
                        loc.DecalType = Library.GetDecalType("zone-s").ID;
                }
            }
        }

        public virtual void Update(DwarfTime Time)
        {
            ZoneBodies.RemoveAll(body => body.IsDead);
        }

        public void CompleteRoomImmediately(List<VoxelHandle> Voxels)
        {
            foreach (var voxel in Voxels)
                AddVoxel(voxel);
            IsBuilt = true;
            OnBuilt();
        }

    }
}