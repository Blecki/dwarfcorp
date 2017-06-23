// BedRoom.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BedRoom : Room
    {
        public static string BedRoomName { get { return "BedRoom"; } }
        public static RoomData BedRoomData { get { return RoomLibrary.GetData(BedRoomName); } }

        public static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
                {Resource.ResourceTags.Wood, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood, 4)},
            };


            List<RoomTemplate> bedroomTemplates = new List<RoomTemplate>();

            RoomTile[,] bedTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Pillow,
                    RoomTile.Bed
                },
                {
                    RoomTile.None,
                    RoomTile.Open,
                    RoomTile.None
                }
            };

            RoomTile[,] bedAccessories =
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
                    RoomTile.Chair,
                    RoomTile.None
                }
            };
            RoomTemplate bed = new RoomTemplate(PlacementType.All, bedTemplate, bedAccessories);

            RoomTile[,] lampTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Lamp
                }
            };

            RoomTile[,] lampAccessories =
            {
                {
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate lamp = new RoomTemplate(PlacementType.All, lampTemplate, lampAccessories);

            bedroomTemplates.Add(lamp);
            bedroomTemplates.Add(bed);

            return new RoomData(BedRoomName, 0, "Brown Tile", roomResources, bedroomTemplates, 
                new Gui.TileReference("rooms", 6))
            {
                Description = "Dwarves relax and rest here",
                CanBuildAboveGround = false,
                MinimumSideLength = 4,
                MinimumSideWidth = 3
            };
        }

        public BedRoom()
        {
            RoomData = BedRoomData;
        }

        public BedRoom(bool designation, IEnumerable<Voxel> designations, WorldManager world) :
            base(designation, designations, BedRoomData, world)
        {
        }

        public BedRoom(IEnumerable<Voxel> voxels, WorldManager world) :
            base(voxels, BedRoomData, world)
        {
            OnBuilt();
        }

    }
}
