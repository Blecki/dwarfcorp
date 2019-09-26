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
        private static int Counter = 0;

        public List<VoxelHandle> Voxels = new List<VoxelHandle>();
        public List<GameComponent> ZoneBodies = new List<GameComponent>();
        public ZoneType Type;
        [JsonIgnore] public Gui.Widget GuiTag;
        public bool IsBuilt;
        public virtual String GetDescriptionString() { return Library.GetString("generic-room-description"); }
        public bool SupportsFilters = false;
        
        [JsonProperty]
        protected int ResPerVoxel = 32;

        [JsonProperty]
        public int ResourceCapacity { get; private set; }

        [JsonIgnore]
        public int ResourcesPerVoxel
        {
            get { return ResPerVoxel; }
            set { ResPerVoxel = value; RecalculateMaxResources(); }
        }
        
        [JsonIgnore]
        public WorldManager World { get; set; }

        protected ChunkManager Chunks { get { return World.ChunkManager; } }

        public ResourceContainer Resources { get; set; }

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

        public Zone(ZoneType Type, WorldManager World)
        {
            this.World = World;
            this.Type = Type;

            ID = Counter + ". " + Type.Name;
            ++Counter;

            Resources = new ResourceContainer
            {
                //MaxResources = 1
            };
        }

        public Zone()
        {

        }

        public GameComponent GetNearestBody(Vector3 location)
        {
            GameComponent toReturn = null;
            float nearestDistance = float.MaxValue;

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
            foreach (var obj in ZoneBodies)
            {
                SetDisplayColor(obj, color);
            }
        }

        private void SetDisplayColor(GameComponent body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>())
                sprite.VertexColorTint = color;
        }

        public GameComponent GetNearestBodyWithTag(Vector3 location, string tag, bool filterReserved)
        {
            GameComponent toReturn = null;
            float nearestDistance = float.MaxValue;

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

        public virtual bool IsFull()
        {
            return Resources.CurrentResourceCount >= ResourceCapacity;
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
            RecalculateMaxResources();
            return removed;
        }

        public virtual void RecalculateMaxResources()
        {
            if (Voxels == null) return;
            int newResources = Voxels.Count * ResourcesPerVoxel;

            if (Resources != null)
            {
                if (newResources < Resources.CurrentResourceCount)
                {
                    while (Resources.CurrentResourceCount > newResources)
                        Resources.RemoveResource(new ResourceAmount(Resources.Enumerate().Where(r => r.Count > 0).First().Type, 1));
                }

                ResourceCapacity = newResources;
                //Resources.MaxResources = newResources;
            }
        }

        public virtual void AddVoxel(VoxelHandle Voxel)
        {
            if(ContainsVoxel(Voxel))
                return;
            Voxel.IsPlayerBuilt = true;
            Voxels.Add(Voxel);

            if (Library.GetVoxelType(Type.FloorType).HasValue(out VoxelType floor))
                Voxel.Type = floor;

            RecalculateMaxResources();
          
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


        public virtual bool AddItem(GameComponent component)
        {
            return AddResource(new ResourceAmount(component));
        }

        public virtual bool AddResource(ResourceAmount Resource)
        {
            return false;
        }

       
        public bool Intersects(BoundingBox box)
        {
            BoundingBox larger = box.Expand(0.1f);
            return Voxels.Any(storage => storage.GetBoundingBox().Intersects(larger));
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

        public bool IsRestingOnZone(Vector3 worldCoordinate, float expansion=1.0f)
        {
            BoundingBox box = GetBoundingBox();
            box.Max.Y += 1;
            box = box.Expand(expansion);
            return box.Contains(worldCoordinate) != ContainmentType.Disjoint;
        }

        public virtual void OnBuilt()
        {

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