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
            Minions = new List<CreatureAI>();
            SelectedMinions = new List<CreatureAI>();
            TaskManager = new TaskManager(this);
            Stockpiles = new List<Stockpile>();
            DigDesignations = new List<BuildOrder>();
            GuardDesignations = new List<BuildOrder>();
            ChopDesignations = new List<Body>();
            AttackDesignations = new List<Body>();
            ShipDesignations = new List<ShipOrder>();
            GatherDesignations = new List<Body>();
            RoomBuilder = new RoomBuilder(this);
            PutDesignator = new PutDesignator(this, TextureManager.GetTexture("TileSet"));
        }


        public Economy Economy { get; set; }
        public ComponentManager Components { get; set; }

        public List<BuildOrder> DigDesignations { get; set; }
        public List<BuildOrder> GuardDesignations { get; set; }
        public List<Body> ChopDesignations { get; set; }
        public List<Body> AttackDesignations { get; set; }
        public List<Body> GatherDesignations { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAI> Minions { get; set; }
        public List<ShipOrder> ShipDesignations { get; set; }
        public RoomBuilder RoomBuilder { get; set; }
        public PutDesignator PutDesignator { get; set; }
        public Color DigDesignationColor { get; set; }

        public TaskManager TaskManager { get; set; }
        public List<Creature> Threats { get; set; }

        public string Name { get; set; }
        public string Alliance { get; set; }
        public List<CreatureAI> SelectedMinions { get; set; }

        public void CollideMinions(GameTime time)
        {
            foreach (CreatureAI minion in Minions)
            {
                foreach (CreatureAI other in Minions)
                {
                    if (minion == other)
                    {
                        continue;
                    }

                    Vector3 meToOther = other.Position - minion.Position;
                    float dist = (meToOther).Length();

                    if (dist < 0.25f)
                    {
                        other.Physics.ApplyForce(meToOther / (dist + 0.01f) * 100, (float)time.ElapsedGameTime.TotalSeconds);
                    }
                }
            }

        }


        public void Update(GameTime time)
        {
            RoomBuilder.CheckRemovals();

            Minions.RemoveAll(m => m.IsDead);
            SelectedMinions.RemoveAll(m => m.IsDead);

            CollideMinions(time);

            List<BuildOrder> removals = (from d in DigDesignations
                                          let vref = d.Vox
                                          let v = vref.GetVoxel(false)
                                          where v == null || v.Health <= 0.0f || v.Type.Name == "empty"
                                          select d).ToList();

            foreach (BuildOrder v in removals)
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
            foreach (BuildOrder d in GuardDesignations)
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

            foreach (BuildOrder v in removals)
            {
                GuardDesignations.Remove(v);
            }

            List<Body> treesToRemove = ChopDesignations.Where(tree => tree.IsDead).ToList();

            foreach (Body tree in treesToRemove)
            {
                ChopDesignations.Remove(tree);
            }

            List<Body> attacksToRemove = AttackDesignations.Where(body => body.IsDead).ToList();

            foreach (Body body in attacksToRemove)
            {
                AttackDesignations.Remove(body);
            }

            HandleThreats();
        }

        public bool IsTaskAssigned(Task task)
        {
            return Minions.Any(minion => minion.Tasks.Contains(task));
        }

        public void HandleThreats()
        {
            List<Task> tasks = new List<Task>();
            List<Creature> threatsToRemove = new List<Creature>();
            foreach (Creature threat in Threats)
            {
                if (threat != null && !threat.IsDead)
                {
                    Task g = new KillEntityTask(threat.Physics);

                    if (!IsTaskAssigned(g))
                    {
                        if (!AttackDesignations.Contains(threat.Physics))
                        {
                            AttackDesignations.Add(threat.Physics);
                        }
                        tasks.Add(g);
                    }
                    else
                    {
                        threatsToRemove.Add(threat);
                    }
                }
                else
                {
                    threatsToRemove.Add(threat);
                }
            }

            foreach (Creature threat in threatsToRemove)
            {
               Threats.Remove(threat);
            }

            DwarfCorp.TaskManager.AssignTasks(tasks, Minions);
        }

        public List<Room> GetRooms()
        {
            return RoomBuilder.DesignatedRooms;
        }

        public void OnVoxelDestroyed(Voxel v)
        {
            if(v == null)
            {
                return;
            }

            VoxelRef voxelRef = v.GetReference();

            RoomBuilder.OnVoxelDestroyed(v);

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

        public int ComputeStockpileCapacity()
        {
            int space = 0;
            foreach (Stockpile pile in Stockpiles)
            {
                space += pile.Resources.MaxResources;
            }

            return space;
        }

        public int ComputeStockpileSpace()
        {
            int space = 0;
            foreach(Stockpile pile in Stockpiles)
            {
                space += pile.Resources.MaxResources - pile.Resources.CurrentResourceCount;
            }

            return space;
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

            ShipDesignations.Add(new ShipOrder(resource, port));
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

        public BuildOrder GetClosestDigDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            BuildOrder closestVoxel = null;
            foreach(BuildOrder designation in DigDesignations)
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

        public BuildOrder GetClosestGuardDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            BuildOrder closestVoxel = null;
            foreach(BuildOrder designation in GuardDesignations)
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

        public BuildOrder GetGuardDesignation(Voxel vox)
        {
            return (from d in GuardDesignations
                let vref = d.Vox
                let v = vref.GetVoxel(false)
                where vox == v
                select d).FirstOrDefault();
        }

        public BuildOrder GetDigDesignation(Voxel vox)
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

        public bool AddResources(ResourceAmount resources)
        {
            ResourceAmount amount = new ResourceAmount(resources.ResourceType, resources.NumResources);
            foreach (Stockpile stockpile in Stockpiles)
            {
                int space = stockpile.Resources.MaxResources - stockpile.Resources.CurrentResourceCount;

                if(space >= amount.NumResources)
                {
                    stockpile.Resources.AddResource(amount);
                    stockpile.HandleBoxes();
                    return true;
                }
                else
                {
                    stockpile.Resources.AddResource(amount);
                    amount.NumResources -= space;
                    stockpile.HandleBoxes();
                    if(amount.NumResources == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
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
            return RoomBuilder.DesignatedRooms.Where(room => room.Intersects(v)).ToList();
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
            zones.AddRange(RoomBuilder.DesignatedRooms);
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
            zones.AddRange(RoomBuilder.DesignatedRooms);
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

        public List<ResourceAmount> ListResources()
        {
            List<ResourceAmount> toReturn = new List<ResourceAmount>();

            foreach(Stockpile stockpile in Stockpiles)
            {
                toReturn.AddRange(stockpile.Resources);
            }

            return toReturn;
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

        public void Hire(Applicant currentApplicant)
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomType.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return;
            }

            Economy.CurrentMoney -= currentApplicant.Level.Pay*4;
            Dwarf newMinion =
                EntityFactory.GenerateDwarf(
                    rooms.First().GetBoundingBox().Center() + Vector3.UnitY * 15,
                    Components, GameState.Game.Content, GameState.Game.GraphicsDevice, PlayState.ChunkManager,
                    PlayState.Camera, this, PlayState.PlanService, "Dwarf", currentApplicant.Class, currentApplicant.Level.Index).GetChildrenOfType<Dwarf>().First();

            newMinion.Stats.CurrentClass = currentApplicant.Class;
            newMinion.Stats.LevelIndex = currentApplicant.Level.Index - 1;
            newMinion.Stats.LevelUp();
            newMinion.Stats.FirstName = currentApplicant.Name.Split(' ')[0];
            newMinion.Stats.LastName = currentApplicant.Name.Split(' ')[1];

            PlayState.AnnouncementManager.Announce("New Hire!" ,currentApplicant.Name + " was hired as a " + currentApplicant.Level.Name);

        }

        public void DispatchBalloon()
        {
            List<Room> rooms = GetRooms().Where(room => room.RoomType.Name == "BalloonPort").ToList();

            if (rooms.Count == 0)
            {
                return;
            }

            Vector3 pos = rooms.First().GetBoundingBox().Center();
            EntityFactory.CreateBalloon(pos + new Vector3(0, 1000, 0), pos + Vector3.UnitY * 15, Components, GameState.Game.Content, GameState.Game.GraphicsDevice, new ShipmentOrder(0, null), this);
        }
    }

}