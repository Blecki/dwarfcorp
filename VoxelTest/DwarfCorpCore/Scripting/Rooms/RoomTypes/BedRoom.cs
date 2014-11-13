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
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> bedroomResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            ResourceAmount woodRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Wood],
                NumResources = 1
            };
            bedroomResources[ResourceLibrary.ResourceType.Wood] = woodRequired;

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
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(BedRoomName, 0, "BrownTileFloor", bedroomResources, bedroomTemplates, new ImageFrame(roomIcons, 16, 2, 1))
            {
                Description = "Dwarves relax and rest here",
                CanBuildAboveGround = false
            };
        }

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

    }
}
