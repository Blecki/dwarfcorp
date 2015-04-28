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
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> roomResources = new Dictionary
                <ResourceLibrary.ResourceType, ResourceAmount>()
            {
                {ResourceLibrary.ResourceType.Stone, new ResourceAmount(ResourceLibrary.ResourceType.Stone)},
                {ResourceLibrary.ResourceType.Mana, new ResourceAmount(ResourceLibrary.ResourceType.Mana)},
            };

            List<RoomTemplate> libraryTemplates = new List<RoomTemplate>();
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

            RoomTile[,]  bookshlf =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.None,
                    RoomTile.BookShelf
                }
            };

            RoomTile[,] bookshlfAcc =
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
            RoomTile[,] bookTemp =
            {
                {
                    RoomTile.None,
                    RoomTile.Open,
                    RoomTile.None
                },
                {
                    RoomTile.Open,
                    RoomTile.BookTable,
                    RoomTile.Open
                },
                {
                    RoomTile.None,
                    RoomTile.Open,
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

            RoomTemplate book = new RoomTemplate(PlacementType.Random, bookTemp, bookAcc);


            libraryTemplates.Add(new RoomTemplate(PlacementType.Random, bookshlf, bookshlfAcc) { Probability = 0.15f});
            libraryTemplates.Add(lamp);
            libraryTemplates.Add(book);
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(LibraryRoomName, 4, "BlueTileFloor", roomResources, libraryTemplates, new ImageFrame(roomIcons, 16, 0, 1))
            {
                Description = "Wizards do magical research here. Also holds mana crystals to charge magic spells.",
                CanBuildAboveGround = false
            };
        }

        public LibraryRoom()
        {
            RoomData = LibraryRoomData;
        }

        public LibraryRoom(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, LibraryRoomData, chunks)
        {
        }

        public LibraryRoom(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, LibraryRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
