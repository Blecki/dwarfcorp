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

using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Using this tool, the player can build items and rooms
    ///     It consists of two parts: a GUI component for
    ///     selecting the kind of room/item and resources to use, and a click-and-drag
    ///     selection tool for specifying where rooms or items are to be built.
    ///     It also silently wraps RoomBuilder, WallBuilder and CraftBuilder for building
    ///     Rooms, Walls and Items. These were older legacy tools that are unified by this class.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuildTool : PlayerTool
    {
        /// <summary>
        ///     The GUI component for selecting rooms/items
        /// </summary>
        public BuildMenu BuildPanel { get; set; }

        /// <summary>
        ///     The type (Room, Item, ec.) to be built.
        /// </summary>
        public BuildMenu.BuildType BuildType { get; set; }

        /// <summary>
        ///     Called whenever the player selects voxels.
        /// </summary>
        /// <param name="voxels">The voxels selected by the player.</param>
        /// <param name="button"></param>
        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
            Player.Faction.RoomBuilder.VoxelsSelected(voxels, button);
            Player.Faction.WallBuilder.VoxelsSelected(voxels, button);
            Player.Faction.CraftBuilder.VoxelsSelected(voxels, button);
        }

        /// <summary>
        ///     Called when the tool starts. Opens up a build menu if none exists already.
        /// </summary>
        public override void OnBegin()
        {
            if (BuildPanel != null)
            {
                BuildPanel.Destroy();
            }
            int w = 600;
            int h = 350;
            BuildPanel = new BuildMenu(PlayState.GUI, PlayState.GUI.RootComponent, Player, BuildType)
            {
                LocalBounds =
                    new Rectangle(GameState.Game.GraphicsDevice.Viewport.Width/2 - w/2,
                        GameState.Game.GraphicsDevice.Viewport.Height/2 - h/2, w, h),
                IsVisible = true,
                DrawOrder = 2
            };
            BuildPanel.TweenIn(Drawer2D.Alignment.Right, 0.25f);
        }

        /// <summary>
        ///     Called when the tool ends. Causes the build menu to disappear.
        /// </summary>
        public override void OnEnd()
        {
            BuildPanel.TweenOut(Drawer2D.Alignment.Right, 0.25f);
        }

        /// <summary>
        ///     Updates the tool
        /// </summary>
        /// <param name="game">The game</param>
        /// <param name="time">The current time</param>
        public override void Update(DwarfGame game, DwarfTime time)
        {
            // If the player is rotating the camera, don't bother using the tool.
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                Player.BodySelector.Enabled = false;
                return;
            }


            // Determine if the player is cooking or doing something else.
            bool hasCook = BuildType == BuildMenu.BuildType.Cook;

            // If the player is not cooking, use the build icon for the mouse pointer.
            if (!hasCook)
            {
                Player.VoxSelector.Enabled = true;
                Player.BodySelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = true;

                PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver()
                    ? GUISkin.MousePointer.Pointer
                    : GUISkin.MousePointer.Build;
            }
                // Otherwise, use the cook icon.
            else
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = true;

                PlayState.GUI.MouseMode = PlayState.GUI.IsMouseOver()
                    ? GUISkin.MousePointer.Pointer
                    : GUISkin.MousePointer.Cook;
            }
        }

        /// <summary>
        ///     Render any debug information needed for the tool.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="graphics">The graphics device.</param>
        /// <param name="time">The current time.</param>
        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            Player.Faction.RoomBuilder.Render(time, PlayState.ChunkManager.Graphics);
        }

        /// <summary>
        ///     Called whenever the player selects entities on the screen. Does nothing for this tool.
        /// </summary>
        /// <param name="bodies">The bodies selected.</param>
        /// <param name="button">The mouse button (left/right/middle) used.</param>
        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
        }

        /// <summary>
        ///     Called whenever the player is drag-selecting voxels with a mouse button pressed.
        ///     Note that this is NOT called when the player has released the mouse button, but
        ///     only while the mouse button is pressed.
        /// </summary>
        /// <param name="voxels">The voxels currently being selected.</param>
        /// <param name="button">The mouse button that is currently down.</param>
        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {
            Player.Faction.RoomBuilder.OnVoxelsDragged(voxels, button);
        }
    }
}