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

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A room containing beds and furniture that dwarves sleep in.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BedRoom : Room
    {
        public BedRoom()
        {
            RoomData = BedRoomData;
        }

        public BedRoom(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, BedRoomData, chunks)
        {
        }

        public BedRoom(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, BedRoomData, chunks)
        {
            OnBuilt();
        }

        /// <summary>
        ///     I kept mispelling "BedRoom" so I just made it a static var.
        /// </summary>
        public static string BedRoomName
        {
            get { return "BedRoom"; }
        }

        public static RoomData BedRoomData
        {
            get { return RoomLibrary.GetData(BedRoomName); }
        }

        /// <summary>
        ///     Initializes the descriptor for all bed rooms.
        /// </summary>
        /// <returns>Data describing how to create a bed room.</returns>
        public static RoomData InitializeData()
        {
            // Resources required for a bed room.
            var roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>
            {
                {Resource.ResourceTags.Wood, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Wood)},
            };


            var bedroomTemplates = new List<RoomTemplate>();

            // Beds must be placed next to a wall with at least one open space nearby.
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

            // We can put a chair next to a bed if we like.
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
            var bed = new RoomTemplate(PlacementType.All, bedTemplate, bedAccessories);

            // Lamps get generated in the bed room corners.
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

            var lamp = new RoomTemplate(PlacementType.All, lampTemplate, lampAccessories);

            bedroomTemplates.Add(lamp);
            bedroomTemplates.Add(bed);

            // Create info about generating the room.
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(BedRoomName, 0, "BrownTileFloor", roomResources, bedroomTemplates,
                new ImageFrame(roomIcons, 16, 2, 1))
            {
                Description = "Dwarves relax and rest here",
                CanBuildAboveGround = false
            };
        }
    }
}