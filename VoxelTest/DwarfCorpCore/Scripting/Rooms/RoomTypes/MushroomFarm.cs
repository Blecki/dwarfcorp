using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class MushroomFarm : Farm
    {
        public static string MushroomFarmName { get { return "MushroomFarm"; } }
        public static RoomData MushroomFarmData { get { return RoomLibrary.GetData(MushroomFarmName); } }

        public static RoomData InitializeData()
        {
            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> mushroomFarmResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            mushroomFarmResources[ResourceLibrary.ResourceType.Mushroom] = new ResourceAmount()
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Mushroom],
                NumResources = 1
            };

            List<RoomTemplate> mushroomTemplates = new List<RoomTemplate>();
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(MushroomFarmName, 6, "TilledSoil", mushroomFarmResources, mushroomTemplates, new ImageFrame(roomIcons, 16, 3, 1))
            {
                Description = "Dwarves can grow mushrooms below ground here",
                CanBuildAboveGround = false,
                MustBeBuiltOnSoil = false,
                MinimumSideLength = 3,
                MinimumSideWidth = 2
            };
        }




        public MushroomFarm()
        {
            RoomData = MushroomFarmData;
        }

        public MushroomFarm(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, MushroomFarmData, chunks)
        {
        }

        public MushroomFarm(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, MushroomFarmData, chunks)
        {
     
        }
        public override Body CreatePlant(Vector3 position)
        {
            return (Body)EntityFactory.CreateEntity<Body>("Mushroom", position);
        }
        
    }
}
