using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Graveyard : Stockpile
    {
        [ZoneFactory("Graveyard")]
        private static Zone _factory(ZoneType Data, WorldManager World)
        {
            return new Graveyard(Data, World);
        }

        public Graveyard()
        {
            SupportsFilters = false;
            BoxType = "Grave";
        }

        public override string GetDescriptionString()
        {
            return "Graveyard " + ID + " - " + Resources.CurrentResourceCount + " of " + Voxels.Count + " plots filled.";
        }

        private Graveyard(ZoneType Data, WorldManager World) :
            base(Data, World)
        {
            Resources = new ResourceContainer();

            WhitelistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse
            };

            BlacklistResources = new List<Resource.ResourceTags>();
            BoxType = "Grave";
            BoxOffset = new Vector3(0.5f, 0.4f, 0.5f);
            ResourcesPerVoxel = 1;
            SupportsFilters = false;
        }

        public override void OnBuilt()
        {
            foreach (var fence in  Fence.CreateFences(World.ComponentManager, ContentPaths.Entities.DwarfObjects.fence, Voxels, false))
            {
                AddBody(fence);
                fence.Manager.RootComponent.AddChild(fence);
            }
        }
    }
}
