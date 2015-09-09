// BuildTool.cs
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
    /// <summary>
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FarmTool : PlayerTool
    {
        public FarmingPanel FarmPanel { get; set; }
        public BuildMenu.BuildType BuildType { get; set; }
        public string PlantType { get; set; }

        public enum FarmMode
        {
            Tilling,
            Planting,
            Harvesting
        }

        public FarmMode Mode { get; set; }


        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            switch (Mode)
            {
                case FarmMode.Tilling:
                    foreach (Voxel voxel in voxels)
                    {
                        if (!voxel.Type.IsSoil)
                        {
                            PlayState.GUI.ToolTipManager.Popup("Can only till soil!");
                            break;
                        }
                        voxel.Type = VoxelLibrary.GetVoxelType("TilledSoil");
                        voxel.Chunk.ShouldRebuild = true;
                    }
                    break;
                case FarmMode.Planting:
                    foreach (Voxel voxel in voxels)
                    {
                        if (voxel.TypeName != "TilledSoil")
                        {
                            PlayState.GUI.ToolTipManager.Popup("Can only plant on tilled soil!");
                            continue;
                        }

                        if (ResourceLibrary.Resources[PlantType].Tags.Contains(Resource.ResourceTags.AboveGroundPlant))
                        {
                            if (voxel.SunColor == 0)
                            {
                                PlayState.GUI.ToolTipManager.Popup("Can only plant " + PlantType + " above ground.");
                                continue;
                            }
                        }
                        else if (
                            ResourceLibrary.Resources[PlantType].Tags.Contains(
                                Resource.ResourceTags.BelowGroundPlant))
                        {
                            if (voxel.SunColor > 0)
                            {
                                PlayState.GUI.ToolTipManager.Popup("Can only plant " + PlantType + " below ground.");
                                continue;
                            }
                        }

                        EntityFactory.CreateEntity<Body>(ResourceLibrary.Resources[PlantType].PlantToGenerate, voxel.Position + Vector3.Up * 1.5f);
                    }
                    break;
            }
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (Mode == FarmMode.Harvesting)
            {
                foreach (Body body in bodies)
                {
                   body.Die();
                }
            }
        }


        public override void OnBegin()
        {
            if (FarmPanel != null)
            {
                FarmPanel.Destroy();
            }
            int w = 600;
            int h = 350;
            FarmPanel = new FarmingPanel(PlayState.GUI, PlayState.GUI.RootComponent)
            {
                LocalBounds = new Rectangle(PlayState.Game.GraphicsDevice.Viewport.Width / 2 - w / 2, PlayState.Game.GraphicsDevice.Viewport.Height / 2 - h / 2, w, h),
                IsVisible = true,
                DrawOrder = 2
            };
            FarmPanel.OnHarvest += FarmPanel_OnHarvest;
            FarmPanel.OnPlant += FarmPanel_OnPlant;
            FarmPanel.OnTill += FarmPanel_OnTill;
            FarmPanel.TweenIn(Drawer2D.Alignment.Right, 0.25f);
        }

        void FarmPanel_OnTill()
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to till soil.");
            Mode = FarmMode.Tilling;
        }

        void FarmPanel_OnPlant(string plantType)
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to plant " + plantType + ".");
            Mode = FarmMode.Planting;
            PlantType = plantType;
        }

        void FarmPanel_OnHarvest()
        {
            PlayState.GUI.ToolTipManager.Popup("Click and drag to harvest.");
            Mode = FarmMode.Harvesting;
        }

        public override void OnEnd()
        {
            FarmPanel.TweenOut(Drawer2D.Alignment.Right, 0.25f);
        }


        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }

            switch (Mode)
            {
               case FarmMode.Tilling:
                    Player.VoxSelector.Enabled = true;
                    Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    Player.BodySelector.Enabled = false;
                    break;
                case FarmMode.Planting:
                    Player.VoxSelector.Enabled = true;
                    Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                    Player.BodySelector.Enabled = false;
                    break;
                case FarmMode.Harvesting:
                    Player.VoxSelector.Enabled = false;
                    Player.BodySelector.Enabled = true;
                    break;
            }
            PlayState.GUI.IsMouseVisible = true;

            PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver() ? GUISkin.MousePointer.Pointer : GUISkin.MousePointer.Build;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
       
        }

    }
}
