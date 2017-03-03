// WorkshopRoom.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Kitchen : Room
    {
        public static string KitchenName { get { return "Kitchen"; } }
        public static RoomData KitchenRoomData { get { return RoomLibrary.GetData(KitchenName); } }

        public static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
                {Resource.ResourceTags.Stone, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Stone)},
                {Resource.ResourceTags.Fuel, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fuel)},
            };

            List<RoomTemplate> workshopTemplates = new List<RoomTemplate>();

            RoomTile[,] template =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.KitchenTable,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTile[,] accessories =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.Barrel,
                    RoomTile.None
                }
            };


            RoomTile[,] stovetemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Stove,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] stoveacc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate barrel = new RoomTemplate(PlacementType.All, template, accessories)
            {
                Probability = 0.2f
            };

            RoomTemplate stove = new RoomTemplate(PlacementType.All, stovetemp, stoveacc)
            {
                Probability = 1.0f
            };
            
            workshopTemplates.Add(stove);
            workshopTemplates.Add(barrel);

            return new RoomData(KitchenName, 2, "Blue Tile", roomResources, workshopTemplates, 
                new Gum.TileReference("rooms", 11))
            {
                Description = "Cooking is done here",
                CanBuildAboveGround = false
            };
        }

        public Kitchen()
        {
            RoomData = KitchenRoomData;
        }

        public Kitchen(bool designation, IEnumerable<Voxel> designations, WorldManager chunks) :
            base(designation, designations, KitchenRoomData, chunks)
        {
        }

        public Kitchen(IEnumerable<Voxel> voxels, WorldManager world) :
            base(voxels, KitchenRoomData, world)
        {
            OnBuilt();
        }

    }
}
