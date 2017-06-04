// BalloonPort.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Graveyard : Stockpile
    {
        [JsonIgnore]
        public static string GraveyardName { get { return "Graveyard"; } }
        [JsonIgnore]
        public static RoomData GraveyardData { get { return RoomLibrary.GetData(GraveyardName); } }

        public new static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> resources = 
                new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            resources[Resource.ResourceTags.Soil] = new Quantitiy<Resource.ResourceTags>()
            {
                ResourceType = Resource.ResourceTags.Soil,
                NumResources = 1
            };


            return new RoomData(GraveyardName, 11, "TilledSoil", resources, new List<RoomTemplate>(), new Gum.TileReference("rooms", 12))
            {
                Description = "Dwarves bury the dead here."
            };
        }

        public Graveyard()
        {
            WhitelistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse
            };
            BlacklistResources = new List<Resource.ResourceTags>();
            BoxType = "Grave";
            BoxOffset = new Vector3(0.5f, 0.6f, 0.5f);
            ResourcesPerVoxel = 1;
        }

        public Graveyard(Faction faction, bool designation, IEnumerable<Voxel> designations, WorldManager world) :
            base(faction, designation, designations, GraveyardData, world)
        {
            WhitelistResources = new List<Resource.ResourceTags>()
            {
                Resource.ResourceTags.Corpse
            };
            BlacklistResources = new List<Resource.ResourceTags>();
            BoxType = "Grave";
            BoxOffset = new Vector3(0.5f, 0.6f, 0.5f);
            ResourcesPerVoxel = 1;
        }

        public Graveyard(Faction faction, IEnumerable<Voxel> voxels, WorldManager world) :
            base(faction, voxels, GraveyardData, world)
        {
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
            ZoneBodies.AddRange(Fence.CreateFences(Faction.World.ComponentManager, ContentPaths.Entities.DwarfObjects.fence, Designations, false));
        }

    }
}
