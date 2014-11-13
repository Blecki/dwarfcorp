using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BalloonPort : Room
    {
        public static string BalloonPortName { get { return "BalloonPort"; } }
        public static RoomData BalloonPortData { get { return RoomLibrary.GetData(BalloonPortName); } }

        public static RoomData InitializeData()
        {
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> balloonPortResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            ResourceAmount balloonStoneRequired = new ResourceAmount
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Wood],
                NumResources = 1
            };
            balloonPortResources[ResourceLibrary.ResourceType.Wood] = balloonStoneRequired;

            RoomTile[,] flagTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Flag
                }
            };

            RoomTile[,] flagAccesories =
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

            RoomTemplate flag = new RoomTemplate(PlacementType.All, flagTemplate, flagAccesories);


            List<RoomTemplate> balloonTemplates = new List<RoomTemplate>
            {
                flag
            };
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(BalloonPortName, 0, "Stockpile", balloonPortResources, balloonTemplates, new ImageFrame(roomIcons, 16, 1, 0))
            {
                Description = "Balloons pick up / drop off resources here.",
                CanBuildBelowGround = false
            };
        }

        public BalloonPort()
        {
            RoomData = BalloonPortData;
        }

        public BalloonPort(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, BalloonPortData, chunks)
        {
        }

        public BalloonPort(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, BalloonPortData, chunks)
        {
            OnBuilt();
        }

    }
}
