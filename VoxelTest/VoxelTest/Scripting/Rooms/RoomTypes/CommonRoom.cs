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
            ResourceAmount woodRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Wood],
                NumResources = 1
            };
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> commonRoomResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            commonRoomResources[ResourceLibrary.ResourceType.Wood] = woodRequired;

            ResourceAmount stoneRquired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Stone],
                NumResources = 1
            };

            commonRoomResources[ResourceLibrary.ResourceType.Stone] = stoneRquired;


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
            return new RoomData(CommonRoomName, 1, "CobblestoneFloor", commonRoomResources, commonRoomTemplates, new ImageFrame(roomIcons, 16, 2, 0))
            {
                Description = "Dwarves come here to socialize and drink",
                CanBuildAboveGround = false
            };

        }

        public CommonRoom()
        {
            RoomData = CommonRoomData;
        }

        public CommonRoom(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, CommonRoomData, chunks)
        {
        }

        public CommonRoom(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, CommonRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
