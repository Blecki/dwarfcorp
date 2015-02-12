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
    public class WheatFarm : Farm
    {
        public static string WheatFarmName { get { return "WheatFarm"; } }
        public static RoomData WheatFarmData { get { return RoomLibrary.GetData(WheatFarmName); } }

        public static RoomData InitializeData()
        {
            List<RoomTemplate> wheatTemplates = new List<RoomTemplate>();

            Dictionary<ResourceLibrary.ResourceType, ResourceAmount> wheatFarmResources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>();
            wheatFarmResources[ResourceLibrary.ResourceType.Grain] = new ResourceAmount()
            {
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Grain],
                NumResources = 1
            };

            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(WheatFarmName, 5, "TilledSoil", wheatFarmResources, wheatTemplates, new ImageFrame(roomIcons, 16, 0, 2))
            {
                Description = "Dwarves can grow wheat above ground here",
                CanBuildBelowGround = false,
                MustBeBuiltOnSoil = true,
                MinimumSideLength = 3,
                MinimumSideWidth = 2
            };
        }

        public WheatFarm()
        {
            RoomData = WheatFarmData;
        }

        public WheatFarm(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, WheatFarmData, chunks)
        {
        }

        public WheatFarm(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, WheatFarmData, chunks)
        {
          
        }

        public override Body CreatePlant(Vector3 position)
        {
            return EntityFactory.CreateEntity<Body>("Wheat", position);
        }

        public override void OnBuilt()
        {
           
            Button farmButton = new Button(PlayState.GUI, PlayState.GUI.RootComponent, "Farm", PlayState.GUI.DefaultFont,
                Button.ButtonMode.ImageButton, new NamedImageFrame(ContentPaths.GUI.icons, 32, 5, 1))
            {
                LocalBounds = new Rectangle(0, 0, 32, 32),
                DrawFrame = true,
                TextColor = Color.White
            };
            farmButton.OnClicked += farmButton_OnClicked;
            GUIObject = new WorldGUIObject(PlayState.ComponentManager.RootComponent, farmButton)
            {
                IsVisible = true,
                LocalTransform = Matrix.CreateTranslation(GetBoundingBox().Center())
            };
            base.OnBuilt();
        }

        void farmButton_OnClicked()
        {
            
        }

        public override void Destroy()
        {
            if (GUIObject != null)
            {
                GUIObject.Die();   
            }
            base.Destroy();
        }
    }
}
