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
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Using this tool, the player can specify regions of voxels to be
    /// turned into rooms.
    /// </summary>
    public class BuildZoneTool : PlayerTool
    {
        private DestroyZoneTool DestroyZoneTool; // I should probably be fired for this.

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
                Player.Faction.RoomBuilder.VoxelsSelected(voxels, button);
            else
                DestroyZoneTool.OnVoxelsSelected(voxels, button);
        }

        public override void OnBegin()
        {
            Player.Faction.RoomBuilder.OnEnter();

            if (DestroyZoneTool == null)
                DestroyZoneTool = new DestroyZoneTool() { Player = this.Player };
        }

        public override void OnEnd()
        {
            Player.Faction.RoomBuilder.End();
            Player.VoxSelector.Clear();
            Player.Faction.RoomBuilder.OnExit();
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Update(game, time);
            else
            {
                if (Player.IsCameraRotationModeActive())
                {
                    Player.VoxSelector.Enabled = false;
                    Player.World.SetMouse(null);
                    Player.BodySelector.Enabled = false;
                    return;
                }

                Player.VoxSelector.Enabled = true;
                Player.BodySelector.Enabled = false;
                Player.VoxSelector.DrawBox = true;
                Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

                if (Player.World.IsMouseOverGui)
                    Player.World.SetMouse(Player.World.MousePointer);
                else
                    Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));
            }
        }

        // Todo: Why is the graphics device passed in when we have a perfectly good global we're using instead?
        public override void Render(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Render(game, time);
            else
            {
                Player.Faction.RoomBuilder.Render(time, GameState.Game.GraphicsDevice);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.OnVoxelsDragged(voxels, button);
            else
            {
                Player.Faction.RoomBuilder.OnVoxelsDragged(voxels, button);
            }
        }
    }
}
