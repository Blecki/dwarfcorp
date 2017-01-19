﻿// WheatFarm.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /*
    [JsonObject(IsReference = true)]
    public class WheatFarm : Farm
    {
        public static string WheatFarmName { get { return "WheatFarm"; } }
        public static RoomData WheatFarmData { get { return RoomLibrary.GetData(WheatFarmName); } }

        public static RoomData InitializeData()
        {
            List<RoomTemplate> wheatTemplates = new List<RoomTemplate>();

            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> wheatFarmResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            wheatFarmResources[Resource.ResourceTags.Grain] = new Quantitiy<Resource.ResourceTags>()
            {
                ResourceType = Resource.ResourceTags.Grain,
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
           
            Button farmButton = new Button(WorldManager.GUI, WorldManager.GUI.RootComponent, "Farm", WorldManager.GUI.DefaultFont,
                Button.ButtonMode.ImageButton, new NamedImageFrame(ContentPaths.GUI.icons, 32, 5, 1))
            {
                LocalBounds = new Rectangle(0, 0, 32, 32),
                DrawFrame = true,
                TextColor = Color.White,
                DrawOrder = -100
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
     */
}
