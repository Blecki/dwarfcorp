// CommonRoom.cs
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
    public class CommonRoom : Room
    {
        public static string CommonRoomName { get { return "CommonRoom"; } }
        public static RoomData CommonRoomData { get { return RoomLibrary.GetData(CommonRoomName); } }

        public static RoomData InitializeData()
        {
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
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
                {Resource.ResourceTags.Wood, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood)},
                {Resource.ResourceTags.Stone, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Stone)},
            };


            List<RoomTemplate> commonRoomTemplates = new List<RoomTemplate>();

            RoomTile[,] tableTemps =
            {
                {
                    RoomTile.None,
                    RoomTile.Open,
                    RoomTile.None
                },
                {
                    RoomTile.Open,
                    RoomTile.Table,
                    RoomTile.Open
                },
                {
                    RoomTile.None,
                    RoomTile.Open,
                    RoomTile.None
                }
            };

            RoomTile[,] tableAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.Chair,
                    RoomTile.None
                },
                {
                    RoomTile.Chair,
                    RoomTile.None,
                    RoomTile.Chair
                },
                {
                    RoomTile.None,
                    RoomTile.Chair,
                    RoomTile.None
                }
            };
            RoomTemplate table = new RoomTemplate(PlacementType.All, tableTemps, tableAcc);

            commonRoomTemplates.Add(lamp);
            commonRoomTemplates.Add(table);
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(CommonRoomName, 1, "CobblestoneFloor", roomResources, commonRoomTemplates, new ImageFrame(roomIcons, 16, 2, 0))
            {
                Description = "Dwarves come here to socialize and drink",
                CanBuildAboveGround = false
            };

        }

        public CommonRoom()
        {
            RoomData = CommonRoomData;
        }

        public CommonRoom(bool designation, IEnumerable<Voxel> designations, WorldManager chunks) :
            base(designation, designations, CommonRoomData, chunks)
        {
        }

        public CommonRoom(IEnumerable<Voxel> voxels, WorldManager chunks) :
            base(voxels, CommonRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
