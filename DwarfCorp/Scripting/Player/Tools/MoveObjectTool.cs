// GatherTool.cs
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
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// When using this tool, the player can click and drag to move objects around.
    /// </summary>
    public class MoveObjectTool : PlayerTool
    {
        private enum ToolState
        {
            Selecting,
            Dragging
        }

        private ToolState State = ToolState.Selecting;

        private GameComponent SelectedBody { get; set; }
        private bool OverrideOrientation = false;
        private float CurrentOrientation = 0.0f;

        private bool leftPressed = false;
        private bool rightPressed = false;

        private Matrix OrigTransform { get; set; }

        public MoveObjectTool()
        {
        }

        public override void OnBegin()
        {
            State = ToolState.Selecting;
        }

        public override void OnEnd()
        {
            if (SelectedBody != null)
            {
                foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                {
                    tinter.VertexColorTint = Color.White;
                    tinter.Stipple = false;
                }

                if (State == ToolState.Dragging)
                {
                    SelectedBody.LocalTransform = OrigTransform;
                    SelectedBody.PropogateTransforms();
                }
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
           
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public bool CanMove(GameComponent entity)
        {
            return entity.Tags.Contains("Moveable") && !entity.IsReserved;
        }

        public void StartDragging(GameComponent entity)
        {
            SelectedBody = entity;
            OrigTransform = SelectedBody.LocalTransform;
            State = ToolState.Dragging;
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.1f);
            OverrideOrientation = false;
            CurrentOrientation = 0.0f;
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
                return;

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = false;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            MouseState mouse = Mouse.GetState();


            if (State == ToolState.Selecting)
            {
                if (SelectedBody != null)
                    foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                    {
                        tinter.VertexColorTint = Color.White;
                        tinter.Stipple = false;
                    }
                
                SelectedBody = Player.World.ComponentManager.SelectRootBodiesOnScreen(new Rectangle(mouse.X, mouse.Y, 1, 1), Player.World.Camera)
                    .Where(body => body.Tags.Contains("Moveable"))
                    .FirstOrDefault();

                if (SelectedBody != null)
                {
                    if (SelectedBody.IsReserved)
                        Player.World.ShowTooltip("Can't move this " + SelectedBody.Name + "\nIt is being used.");
                    else
                    {
                        Player.World.ShowTooltip("Left click and drag to move this " + SelectedBody.Name);
                        foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                        {
                            tinter.VertexColorTint = Color.Blue;
                            tinter.Stipple = false;
                        }
                    }

                    if (mouse.LeftButton == ButtonState.Pressed)
                    {
                        StartDragging(SelectedBody);
                    }
                }
            }
            else if (State == ToolState.Dragging)
            {
                if (SelectedBody == null) throw new InvalidProgramException();

                var craftDetails = SelectedBody.GetRoot().GetComponent<CraftDetails>();
                if (craftDetails != null && CraftLibrary.GetCraftable(craftDetails.CraftType).AllowRotation)
                {
                    HandleOrientation();
                    Player.World.ShowTooltip(String.Format("Press {0}/{1} to rotate.", ControlSettings.Mappings.RotateObjectLeft, ControlSettings.Mappings.RotateObjectRight));
                }

                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                if (voxelUnderMouse.IsValid && voxelUnderMouse.IsEmpty)
                {
                    var spawnOffset = Vector3.Zero;
                    CraftItem craftItem = null;

                    if (craftDetails != null)
                    {
                        craftItem = CraftLibrary.GetCraftable(craftDetails.CraftType);
                        if (craftItem != null)
                            spawnOffset = craftItem.SpawnOffset;
                        else
                            Console.Error.WriteLine("{0} had no craft item.", craftDetails.CraftType);
                    }

                    
                    if (craftItem == null)
                    {
                        return;
                    }

                    SelectedBody.LocalPosition = voxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + spawnOffset;
                    SelectedBody.UpdateTransform();

                    if (OverrideOrientation)
                        SelectedBody.Orient(CurrentOrientation);
                    else
                        SelectedBody.OrientToWalls();

                    SelectedBody.PropogateTransforms();

                    var validPlacement = ObjectHelper.IsValidPlacement(voxelUnderMouse, craftItem, Player, SelectedBody, "move", "moved");

                    foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                    {
                        tinter.VertexColorTint = validPlacement ? Color.Green : Color.Red;
                        tinter.Stipple = true;
                    }

                    if (mouse.LeftButton == ButtonState.Released)
                    {
                        if (validPlacement)
                        {

                        }
                        else
                        {
                            SelectedBody.LocalTransform = OrigTransform;
                            SelectedBody.PropogateTransforms();
                        }

                        foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                        {
                            tinter.VertexColorTint = Color.White;
                            tinter.Stipple = false;
                        }

                        State = ToolState.Selecting;
                    }
                }
            }
        }

        private void HandleOrientation()
        {
            // Don't attempt any camera control if the user is trying to type intoa focus item.
            if (Player.World.Gui.FocusItem != null && !Player.World.Gui.FocusItem.IsAnyParentTransparent() && !Player.World.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }
            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (leftPressed && !leftKey)
            {
                OverrideOrientation = true;
                leftPressed = false;
                CurrentOrientation += (float) (Math.PI/2);
                if (SelectedBody != null)
                {
                    SelectedBody.Orient(CurrentOrientation);
                    SelectedBody.UpdateBoundingBox();
                    SelectedBody.UpdateTransform();
                    SelectedBody.PropogateTransforms();
                    SelectedBody.UpdateBoundingBox();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.5f);
                }
            }

            if (rightPressed && !rightKey)
            {
                OverrideOrientation = true;
                rightPressed = false;
                CurrentOrientation -= (float) (Math.PI/2);
                if (SelectedBody != null)
                {
                    SelectedBody.Orient(CurrentOrientation);
                    SelectedBody.UpdateBoundingBox();
                    SelectedBody.UpdateTransform();
                    SelectedBody.PropogateTransforms();
                    SelectedBody.UpdateBoundingBox();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.5f);
                }
            }

            leftPressed = leftKey;
            rightPressed = rightKey;
        }
        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
