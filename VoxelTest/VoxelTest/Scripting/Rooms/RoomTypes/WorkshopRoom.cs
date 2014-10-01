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
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> roomResources = new Dictionary
                <ResourceLibrary.ResourceType, ResourceAmount>()
            {
                {ResourceLibrary.ResourceType.Iron, new ResourceAmount(ResourceLibrary.ResourceType.Iron)},
                {ResourceLibrary.ResourceType.Coal, new ResourceAmount(ResourceLibrary.ResourceType.Coal)},
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

            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(WorkshopName, 2, "CobblestoneFloor", roomResources, workshopTemplates, new ImageFrame(roomIcons, 16, 1, 1))
            {
                Description = "Craftsdwarves build mechanisms here",
                CanBuildAboveGround = false
            };
        }

        public WorkshopRoom()
        {
            RoomData = WorkshopRoomData;
        }

        public WorkshopRoom(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, WorkshopRoomData, chunks)
        {
        }

        public WorkshopRoom(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, WorkshopRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
