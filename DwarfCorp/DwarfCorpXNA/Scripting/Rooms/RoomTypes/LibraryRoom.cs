// LibraryRoom.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class LibraryRoom : Room
    {
        public static string LibraryRoomName { get { return "LibraryRoom"; } }
        public static RoomData LibraryRoomData { get { return RoomLibrary.GetData(LibraryRoomName); } }

        public static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> roomResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>()
            {
                {Resource.ResourceTags.Magical, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 2)},
                {Resource.ResourceTags.Precious, new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Precious, 1)},
            };


            List<RoomTemplate> libraryTemplates = new List<RoomTemplate>();
            RoomTile[,] lampTemplate =
            {
                {
                    RoomTile.None, RoomTile.Wall | RoomTile.Edge },
                {
                    RoomTile.Wall | RoomTile.Edge, RoomTile.Lamp
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

            RoomTile[,]  bookshlf =
            {
                {
                    RoomTile.Edge | RoomTile.Wall, RoomTile.Edge | RoomTile.Wall, RoomTile.Edge | RoomTile.Wall,
                },
                {  
                    RoomTile.Open, RoomTile.BookShelf, RoomTile.Open
                },
            };

            RoomTile[,] bookshlfAcc =
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

            RoomTemplate lamp = new RoomTemplate(PlacementType.All, lampTemplate, lampAccessories);
            RoomTile[,] bookTemp =
            {
                {
                    RoomTile.None,
                    RoomTile.Open | RoomTile.Edge | RoomTile.BookShelf,
                    RoomTile.None
                },
                {
                    RoomTile.Open | RoomTile.Edge | RoomTile.BookShelf,
                    RoomTile.Books,
                    RoomTile.Open | RoomTile.Edge | RoomTile.BookShelf,
                },
                {
                    RoomTile.None,
                    RoomTile.Open | RoomTile.Edge | RoomTile.BookShelf,
                    RoomTile.None
                }
            };

            RoomTile[,] bookAcc =
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

            RoomTemplate book = new RoomTemplate(PlacementType.Random, bookTemp, bookAcc)
            {
                Probability = 0.5f
            };


            libraryTemplates.Add(lamp);
            libraryTemplates.Add(new RoomTemplate(PlacementType.Random, bookshlf, bookshlfAcc) { Probability = 0.15f });
            libraryTemplates.Add(book);
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(LibraryRoomName, 4, "Blue Tile", roomResources, libraryTemplates,
                new Gui.TileReference("rooms", 4))
            {
                Description = "Wizards do magical research here. Also holds mana crystals to charge magic spells.",
                CanBuildAboveGround = false
            };
        }

        public LibraryRoom()
        {
            RoomData = LibraryRoomData;
        }

        public LibraryRoom(bool designation, IEnumerable<Voxel> designations, WorldManager chunks, Faction faction) :
            base(designation, designations, LibraryRoomData, chunks, faction)
        {
        }

        public LibraryRoom(IEnumerable<Voxel> voxels, WorldManager chunks, Faction faction) :
            base(voxels, LibraryRoomData, chunks, faction)
        {
            OnBuilt();
        }

    }
}
