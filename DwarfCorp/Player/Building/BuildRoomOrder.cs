using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies a list of voxels which are to be turned
    /// into a BuildRoom.
    /// </summary>
    public class BuildRoomOrder
    {
        public Zone ToBuild { get; set; }
        public Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> PutResources { get; set; }
        public List<BuildVoxelOrder> VoxelOrders { get; set; }
        public Faction Faction { get; set; }
        public List<GameComponent> WorkObjects = new List<GameComponent>(); 
        public bool IsBuilt { get; set; }
        public float BuildProgress { get; set; }
        [JsonIgnore]
        private WorldManager World { get; set; }
        public bool IsDestroyed { get; set; }
        public CreatureAI ResourcesReservedFor = null;
        [JsonIgnore]
        public Gui.Widget DisplayWidget = null;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            World = (WorldManager)ctx.Context;
        }

        public BuildRoomOrder()
        {
            BuildProgress = 0;
        }


        public BuildRoomOrder(Zone toBuild, Faction faction, WorldManager world)
        {
            BuildProgress = 0;
            World = world;
            ToBuild = toBuild;
            PutResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            VoxelOrders = new List<BuildVoxelOrder>();
            IsBuilt = false;
            Faction = faction;
            IsDestroyed = false;
        }


        public void AddResources(List<Quantitiy<Resource.ResourceTags>> resources)
        {
            foreach (Quantitiy<Resource.ResourceTags> resource in resources)
            {
                if(PutResources.ContainsKey(resource.Type))
                {
                    Quantitiy<Resource.ResourceTags> amount = PutResources[resource.Type];
                    amount.Count += resource.Count;
                }
                else
                {
                    Quantitiy<Resource.ResourceTags> amount = new Quantitiy<Resource.ResourceTags>();
                    amount.Count += resource.Count;
                    amount.Type = resource.Type;

                    PutResources[resource.Type] = amount;
                }
            }
        }


        public bool MeetsBuildRequirements()
        {
            bool toReturn = true;
            foreach (Resource.ResourceTags s in ToBuild.Type.RequiredResources.Keys)
            {
                if (!PutResources.ContainsKey(s))
                {
                    return false;
                }
                else
                {
                    toReturn = toReturn && (PutResources[s].Count >= Math.Max((int)(ToBuild.Type.RequiredResources[s].Count * VoxelOrders.Count * 0.25f), 1));
                }
            }

            return toReturn;
        }

        public virtual void Build(bool silent=false)
        {
            if(IsBuilt)
                return;
            IsBuilt = true;

            RoomLibrary.CompleteRoomImmediately(ToBuild, VoxelOrders.Select(o => o.Voxel).ToList());

            if (!silent)
            {
                World.MakeAnnouncement(String.Format("{0} was built", ToBuild.Type.Name), null);
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
            }

            foreach (GameComponent fence in WorkObjects)
                fence.Die();
        }

        public void Destroy()
        {
            ToBuild.Destroy();
            foreach (GameComponent fence in WorkObjects)
            {
                fence.Die();
            }
            IsDestroyed = true;
        }

        public void SetTint(Color color)
        {
            foreach (var fence in WorkObjects)
            {
                SetDisplayColor(fence, color);
            }
        }

        private void SetDisplayColor(GameComponent body, Color color)
        {
            foreach (var sprite in body.EnumerateAll().OfType<Tinter>().ToList())
                sprite.VertexColorTint = color;
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> components = VoxelOrders.Select(vox => vox.Voxel.GetBoundingBox()).ToList();

            return MathFunctions.GetBoundingBox(components);
        }

        public bool IsResourceSatisfied(Resource.ResourceTags name)
        {
            int required = GetNumRequiredResources(name);
            int current = 0;

            if(PutResources.ContainsKey(name))
            {
                current = (int) PutResources[name].Count;
            }

            return current >= required;
        }

        public int GetNumRequiredResources(Resource.ResourceTags name)
        {
            if(ToBuild.Type.RequiredResources.ContainsKey(name))
            {
                return Math.Max((int) (ToBuild.Type.RequiredResources[name].Count * VoxelOrders.Count * 0.25f), 1);
            }
            else
            {
                return 0;
            }
        }

        public string GetTextDisplay()
        {
            string toReturn = ToBuild.Type.Name;

            foreach (Quantitiy<Resource.ResourceTags> amount in ToBuild.Type.RequiredResources.Values)
            {
                toReturn += "\n";
                int numResource = 0;
                if(PutResources.ContainsKey(amount.Type))
                {
                    numResource = (int) (PutResources[amount.Type].Count);
                }
                toReturn += amount.Type.ToString() + " : " + numResource + "/" + Math.Max((int) (amount.Count * VoxelOrders.Count * 0.25f), 1);
            }

            return toReturn;
        }

        public List<Quantitiy<Resource.ResourceTags>> ListRequiredResources()
        {
            List<Quantitiy<Resource.ResourceTags>> toReturn = new List<Quantitiy<Resource.ResourceTags>>();
            foreach (Resource.ResourceTags s in ToBuild.Type.RequiredResources.Keys)
            {
                int needed = Math.Max((int) (ToBuild.Type.RequiredResources[s].Count * VoxelOrders.Count * 0.25f), 1);
                int currentResources = 0;

                if(PutResources.ContainsKey(s))
                {
                    currentResources = PutResources[s].Count;
                }

                if(currentResources >= needed)
                {
                    continue;
                }

                toReturn.Add(new Quantitiy<Resource.ResourceTags>(s, needed - currentResources));
            }

            return toReturn;
        }
    }

}
