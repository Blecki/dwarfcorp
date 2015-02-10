using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class TrainingRoom : Room
    {
        public static string TrainingRoomName { get { return "TrainingRoom"; } }
        public static RoomData TrainingRoomData { get { return RoomLibrary.GetData(TrainingRoomName); } }

        public static RoomData InitializeData()
        {
            List<RoomTemplate> trainingTemplates = new List<RoomTemplate>();

            RoomTile[,] targetTemp =
            {
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Strawman,
                    RoomTile.Open
                },
                {
                    RoomTile.Open,
                    RoomTile.Open,
                    RoomTile.Open
                }
            };

            RoomTile[,] strawAcc =
            {
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.Target
                },
                {
                    RoomTile.None,
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate straw = new RoomTemplate(PlacementType.All, targetTemp, strawAcc);
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
            trainingTemplates.Add(lamp);
            trainingTemplates.Add(straw);
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> roomResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            ResourceAmount woodRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Stone],
                NumResources = 1
            };
            roomResources[ResourceLibrary.ResourceType.Stone] = woodRequired;
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(TrainingRoomName, 3, "CobblestoneFloor", roomResources, trainingTemplates, new ImageFrame(roomIcons, 16, 3, 0))
            {
                Description = "Military dwarves train here"
            };

        }

        public TrainingRoom()
        {
            RoomData = TrainingRoomData;
        }

        public TrainingRoom(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, TrainingRoomData, chunks)
        {
        }

        public TrainingRoom(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, TrainingRoomData, chunks)
        {
            OnBuilt();
        }

    }
}
