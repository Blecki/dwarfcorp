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
    public class WorkshopRoom : Room
    {
        public static string WorkshopName { get { return "WorkshopRoom"; } }
        public static RoomData WorkshopRoomData { get { return RoomLibrary.GetData(WorkshopName); } }

        public static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
                {Resource.ResourceTags.Metal, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Metal)},
                {Resource.ResourceTags.Fuel, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fuel)},
            };

            List<RoomTemplate> workshopTemplates = new List<RoomTemplate>();

            RoomTile[,] anvilTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Forge,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] anvilAcc =
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
                    RoomTile.Anvil,
                    RoomTile.None
                }
            };

            RoomTemplate anvil = new RoomTemplate(PlacementType.All, anvilTemp, anvilAcc);
            workshopTemplates.Add(anvil);

            return new RoomData(WorkshopName, 2, "Cobble", roomResources, workshopTemplates,
                new Gui.TileReference("rooms", 5))
            {
                Description = "Craftsdwarves build mechanisms here",
                CanBuildAboveGround = false
            };
        }

        public WorkshopRoom()
        {
            RoomData = WorkshopRoomData;
        }

        public WorkshopRoom(bool designation, IEnumerable<Voxel> designations, WorldManager chunks) :
            base(designation, designations, WorkshopRoomData, chunks)
        {
        }

        public WorkshopRoom(IEnumerable<Voxel> voxels, WorldManager chunks) :
            base(voxels, WorkshopRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
