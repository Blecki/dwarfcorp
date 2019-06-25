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
        [RoomFactory("Graveyard")]
        private static Zone _factory(RoomType Data, WorldManager World)
        {
            return new Graveyard(Data, World);
        }

        public Graveyard()
        {
        }

        public override string GetDescriptionString()
        {
            return "Graveyard " + ID + " - " + Boxes.Count + " of " + Voxels.Count + " plots filled.";
        }

        private Graveyard(RoomType Data, WorldManager World) :
            base(Data, World)
        {
            Resources = new ResourceContainer();

            WhitelistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse
            };
            BlacklistResources = new List<Resource.ResourceTags>();
            BoxType = "Grave";
            BoxOffset = new Vector3(0.5f, 0.6f, 0.5f);
            ResourcesPerVoxel = 1;
        }

        public override void OnBuilt()
        {
            foreach (var fence in  Fence.CreateFences(World.ComponentManager,
                ContentPaths.Entities.DwarfObjects.fence, Voxels, false))
            {
                AddBody(fence);
                fence.Manager.RootComponent.AddChild(fence);
            }
        }
    }
}
