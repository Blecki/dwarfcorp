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
        private static Zone _factory(String ZoneTypeName, WorldManager World)
        {
            return new Graveyard(ZoneTypeName, World);
        }

        public Graveyard()
        {
            SupportsFilters = false;
            BoxType = "Grave";
        }

        public override string GetDescriptionString()
        {
            return "Graveyard " + ID + " - " + Resources.TotalCount + " of " + Voxels.Count + " plots filled.";
        }

        private Graveyard(String ZoneTypeName, WorldManager World) :
            base(ZoneTypeName, World)
        {
            Resources = new ResourceSet();

            WhitelistResources = new List<String>()
            {
                "Corpse"
            };

            BlacklistResources = new List<String>();
            BoxType = "Grave";
            BoxOffset = new Vector3(0.5f, 0.4f, 0.5f);
            ResourcesPerVoxel = 1;
            RecalculateMaxResources();
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
