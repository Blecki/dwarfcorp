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
    public class BuildTool : PlayerTool
    {
        private NewGui.BuildMenu BuildPanel;
        public NewGui.BuildMenu.BuildTypes BuildType;

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            Player.Faction.RoomBuilder.VoxelsSelected(voxels, button);
            Player.Faction.WallBuilder.VoxelsSelected(voxels, button);
            Player.Faction.CraftBuilder.VoxelsSelected(voxels, button);
        }

        public override void OnBegin()
        {
            if (BuildPanel != null && BuildPanel.Root != null)
                BuildPanel.Close();

            BuildPanel = WorldManager.NewGui.ConstructWidget(new NewGui.BuildMenu
                {
                    Faction = Player,
                    BuildType = BuildType
                }) as NewGui.BuildMenu;

            WorldManager.NewGui.ShowPopup(BuildPanel, false);

            Player.Faction.CraftBuilder.IsEnabled = false;

            WorldManager.Paused = true;
        }

        public override void OnEnd()
        {
            BuildPanel.Close();
            BuildPanel = null;
            Player.Faction.CraftBuilder.IsEnabled = false;
            Player.Faction.RoomBuilder.OnExit();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                WorldManager.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }


            bool hasCook = BuildType.HasFlag(NewGui.BuildMenu.BuildTypes.Cook);

            if (!hasCook)
            {
                Player.VoxSelector.Enabled = true;
                Player.BodySelector.Enabled = false;

                if (WorldManager.IsMouseOverGui)
                    WorldManager.SetMouse(WorldManager.MousePointer);
                else
                    WorldManager.SetMouse(new Gum.MousePointer("mouse", 1, 4));
            }
            else
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;

                if (WorldManager.IsMouseOverGui)
                    WorldManager.SetMouse(WorldManager.MousePointer);
                else
                    WorldManager.SetMouse(new Gum.MousePointer("mouse", 1, 11));
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            Player.Faction.RoomBuilder.Render(time, WorldManager.ChunkManager.Graphics);
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {
            Player.Faction.RoomBuilder.OnVoxelsDragged(voxels, button);
        }
    }
}
