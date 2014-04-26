using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A faction is an independent collection of creatures, tied to an economy, rooms, and designations.
    /// Examples might be the player's dwarves, or the faction of goblins.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Faction
    {

        public Faction()
        {
            Threats = new List<Creature>();
            Minions = new List<CreatureAIComponent>();
            TaskManager = new TaskManager(this);
            Stockpiles = new List<Stockpile>();
            DigDesignations = new List<Designation>();
            GuardDesignations = new List<Designation>();
            ChopDesignations = new List<Body>();
            ShipDesignations = new List<ShipDesignation>();
            GatherDesignations = new List<Body>();
            RoomDesignator = new RoomDesignator(this);
            PutDesignator = new PutDesignator(this, TextureManager.GetTexture("TileSet"));
        }


        public Economy Economy { get; set; }
        public ComponentManager Components { get; set; }

        public List<Designation> DigDesignations { get; set; }
        public List<Designation> GuardDesignations { get; set; }
        public List<Body> ChopDesignations { get; set; }
        public List<Body> GatherDesignations { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAIComponent> Minions { get; set; }
        public List<ShipDesignation> ShipDesignations { get; set; }
        public RoomDesignator RoomDesignator { get; set; }
        public PutDesignator PutDesignator { get; set; }
        public Color DigDesignationColor { get; set; }

        public TaskManager TaskManager { get; set; }
        public List<Creature> Threats { get; set; }

        public string Name { get; set; }


        public void Update(GameTime time)
        {
            Economy.Update(time);
            RoomDesignator.CheckRemovals();
            TaskManager.AssignTasks();
            TaskManager.ManageTasks();

            List<Designation> removals = (from d in DigDesignations
                                          let vref = d.Vox
                                          let v = vref.GetVoxel(false)
                                          where v == null || v.Health <= 0.0f || v.Type.Name == "empty"
                                          select d).ToList();

            foreach (Designation v in removals)
            {
                DigDesignations.Remove(v);
            }

            List<Body> gatherRemovals = (from b in GatherDesignations
                where b == null || b.IsDead
                select b).ToList();

            foreach(Body b in gatherRemovals)
            {
                GatherDesignations.Remove(b);
            }
            

            removals.Clear();
            foreach (Designation d in GuardDesignations)
            {
                VoxelRef vref = d.Vox;
                Voxel v = vref.GetVoxel(false);

                if (v != null && !(v.Health <= 0.0f) && v.Type.Name != "empty")
                {
                    continue;
                }

                removals.Add(d);

                if (v != null)
                {
                    v.Kill();
                }
            }

            foreach (Designation v in removals)
            {
                GuardDesignations.Remove(v);
            }

            List<Body> treesToRemove = ChopDesignations.Where(tree => tree.IsDead).ToList();

            foreach (Body tree in treesToRemove)
            {
                ChopDesignations.Remove(tree);
            }
        }


        public void OnVoxelDestroyed(Voxel v)
        {
            if(v == null)
            {
                return;
            }

            VoxelRef voxelRef = v.GetReference();

            RoomDesignator.OnVoxelDestroyed(v);

            List<Stockpile> toRemove = new List<Stockpile>();
            foreach(Stockpile s in Stockpiles)
            {
                if(s.ContainsVoxel(voxelRef))
                {
                    s.RemoveVoxel(voxelRef);
                }

                if(s.Voxels.Count == 0)
                {
                    toRemove.Add(s);
                }
            }

            foreach(Stockpile s in toRemove)
            {
                Stockpiles.Remove(s);
                s.Destroy();
            }
        }

        public void AddShipDesignation(ResourceAmount resource, Room port)
        {
            // TODO: Reimplement
            /*
            List<Body> componentsToShip = new List<Body>();

            foreach (Stockpile s in Stockpiles)
            {
                for (int i = componentsToShip.Count; i < resource.NumResources; i++)
                {
                    Body r = s.FindItemWithTag(resource.ResourceType.ResourceName, componentsToShip);

                    if (r != null)
                    {
                        componentsToShip.Add(r);
                    }
                }
            }

            ShipDesignations.Add(new ShipDesignation(resource, port));
             */

        }

        public void AddGatherDesignation(Body resource)
        {
            if (resource.Parent != Components.RootComponent || resource.IsDead)
            {
                return;
            }

            if (!GatherDesignations.Contains(resource))
            {
                GatherDesignations.Add(resource);
            }
        }

        public Designation GetClosestDigDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            Designation closestVoxel = null;
            foreach(Designation designation in DigDesignations)
            {
                VoxelRef vref = designation.Vox;
                Voxel v = vref.GetVoxel(false);

                float d = (v.Position - position).LengthSquared();
                if(!(d < closestDist))
                {
                    continue;
                }

                closestDist = d;
                closestVoxel = designation;
            }

            return closestVoxel;
        }

        public Designation GetClosestGuardDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            Designation closestVoxel = null;
            foreach(Designation designation in GuardDesignations)
            {
                VoxelRef vref = designation.Vox;
                Voxel v = vref.GetVoxel(false);

                float d = (v.Position - position).LengthSquared();
                if(!(d < closestDist))
                {
                    continue;
                }

                closestDist = d;
                closestVoxel = designation;
            }

            return closestVoxel;
        }

        public Designation GetGuardDesignation(Voxel vox)
        {
            return (from d in GuardDesignations
                let vref = d.Vox
                let v = vref.GetVoxel(false)
                where vox == v
                select d).FirstOrDefault();
        }

        public Designation GetDigDesignation(Voxel vox)
        {
            return (from d in DigDesignations
                let vref = d.Vox
                let v = vref.GetVoxel(false)
                where vox == v
                select d).FirstOrDefault();
        }

        public bool IsDigDesignation(Voxel vox)
        {
            return DigDesignations.Select(d => d.Vox).Select(vref => vref.GetVoxel(false)).Any(v => vox == v);
        }

        public bool IsGuardDesignation(VoxelRef vox)
        {
            Voxel voxel = vox.GetVoxel(false);

            return voxel != null && IsGuardDesignation(voxel);
        }

        public bool IsGuardDesignation(Voxel vox)
        {
            return GuardDesignations.Select(d => d.Vox).Select(vref => vref.GetVoxel(false)).Any(v => vox == v);
        }


        public Stockpile GetNearestStockpile(Vector3 position)
        {
            Stockpile nearest = null;

            float closestDist = float.MaxValue;
            foreach(Stockpile stockpile in Stockpiles)
            {
                float dist = (stockpile.GetBoundingBox().Center() - position).LengthSquared();

                if(dist < closestDist)
                {
                    closestDist = dist;
                    nearest = stockpile;
                }
            }

            return nearest;
        }


        public Stockpile GetIntersectingStockpile(Voxel v)
        {
            return Stockpiles.FirstOrDefault(pile => pile.Intersects(v));
        }

        public Stockpile GetIntersectingStockpile(BoundingBox v)
        {
            return Stockpiles.FirstOrDefault(pile => pile.Intersects(v));
        }

        public List<Stockpile> GetIntersectingStockpiles(BoundingBox v)
        {
            return Stockpiles.Where(pile => pile.Intersects(v)).ToList();
        }

        public List<Room> GetIntersectingRooms(BoundingBox v)
        {
            return RoomDesignator.DesignatedRooms.Where(room => room.Intersects(v)).ToList();
        }

        public bool IsInStockpile(Voxel v)
        {
            VoxelRef vRef = v.GetReference();
            return Stockpiles.Any(s => s.ContainsVoxel(vRef));
        }

        public Body GetRandomGatherDesignationWithTag(string tag)
        {
            List<Body> des = GatherDesignations.Where(c => c.Tags.Contains(tag)).ToList();
            return des.Count == 0 ? null : des[PlayState.Random.Next(0, des.Count)];
        }

        public List<Item> FindItemsWithTags(TagList tags)
        {
            List<Item> toReturn = new List<Item>();
            List<Zone> zones = new List<Zone>();
            zones.AddRange(RoomDesignator.DesignatedRooms);
            zones.AddRange(Stockpiles);

            foreach (Zone s in zones)
            {
                // TODO: Reimplement
                //toReturn.AddRange(s.GetItemsWithTags(tags));
            }



            return toReturn;
        }

        public bool HasFreeStockpile()
        {
            return Stockpiles.Any(s => !s.IsFull());
        }

        public Item FindNearestItemWithTags(TagList tags, Vector3 location, bool filterReserved)
        {
            Item closestItem = null;
            float closestDist = float.MaxValue;
            List<Zone> zones = new List<Zone>();
            zones.AddRange(RoomDesignator.DesignatedRooms);
            zones.AddRange(Stockpiles);

            foreach (Zone s in zones)
            {
                // TODO: Reimplement
                /*
                Item i = s.FindNearestItemWithTags(tags, location, filterReserved);

                if (i != null)
                {
                    float d = (i.UserData.GlobalTransform.Translation - location).LengthSquared();
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestItem = i;
                    }
                }
                 */
            }

            

            return closestItem;
        }

        public int CompareZones(Zone a, Zone b, Vector3 pos)
        {
            if(a == b) 
            {
                return 0;
            }
            else
            {
               
                float costA = (pos- a.GetBoundingBox().Center()).LengthSquared();
                float costB = (pos - b.GetBoundingBox().Center()).LengthSquared();

                if(costA < costB)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }


        public bool HasResources(List<ResourceAmount> resources)
        {
            foreach (ResourceAmount resource in resources)
            {
                int count = Stockpiles.Sum(stock => stock.Resources.GetResourceCount(resource.ResourceType));

                if(count < resource.NumResources)
                {
                    return false;
                }
            }

            return true;
        }

        public bool RemoveResources(List<ResourceAmount> resources, Vector3 position)
        {
            if(!HasResources(resources))
            {
                return false;
            }


            List<Stockpile> stockpilesCopy = new List<Stockpile>(Stockpiles);
            stockpilesCopy.Sort((a, b) => CompareZones(a, b, position));


            foreach(ResourceAmount resource in resources)
            {
                int count = 0;
                List<Vector3> positions = new List<Vector3>();
                foreach (Stockpile stock in stockpilesCopy)
                {
                    int num =  stock.Resources.RemoveMaxResources(resource, resource.NumResources - count);
                    stock.HandleBoxes();
                    if(stock.Boxes.Count > 0)
                    {
                        for(int i = 0; i < num; i++)
                        {
                            positions.Add(stock.Boxes[stock.Boxes.Count - 1].LocalTransform.Translation);
                        }
                    }

                    count += num;

                    if(count >= resource.NumResources)
                    {
                        break;
                    }

                }


                foreach(Vector3 vec in positions)
                {
                    Body newEntity = EntityFactory.GenerateComponent(resource.ResourceType.ResourceName, vec + MathFunctions.RandVector3Cube() * 0.5f,
               PlayState.ComponentManager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, PlayState.ComponentManager.Factions, PlayState.Camera);

                    TossMotion toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, position);
                    newEntity.AnimationQueue.Add(toss);
                    toss.OnComplete += () => toss_OnComplete(newEntity);

                }

            }

            return true;
        }

        void toss_OnComplete(Body entity)
        {
            entity.Die();
            
        }
    }

}