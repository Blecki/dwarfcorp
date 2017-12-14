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
        private Body SelectedBody { get; set; }
        private bool mouseDown = false;
        private VoxelHandle prevVoxel = new VoxelHandle();
        private bool OverrideOrientation = false;
        private float CurrentOrientation = 0.0f;

        private bool leftPressed = false;
        private bool rightPressed = false;
        public MoveObjectTool()
        {
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (SelectedBody != null)
            {
                return;
            }

            if (bodies.Count > 0)
            {
                foreach (var body in bodies)
                {
                    if (button == InputManager.MouseButton.Left)
                    {
                        if (Player.Faction.OwnedObjects.Contains(body) && !body.IsReserved &&
                            body.Tags.Any(tag => tag == "Moveable"))
                        {
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, body.Position, 0.1f);
                            SelectedBody = body;
                            Player.World.ShowToolPopup(String.Format("Press {0}/{1} to rotate.", ControlSettings.Mappings.RotateObjectLeft, ControlSettings.Mappings.RotateObjectRight));
                            break;
                        }
                    }
                    else if (Player.Faction.OwnedObjects.Contains(body) && body.Tags.Any(tag => tag == "Moveable"))
                    {
                        if (body.IsReserved)
                        {
                            Player.World.ShowToolPopup(string.Format("Can't move this {0}. It is being used.", body.Name));
                            continue;
                        }
                        body.Delete();
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, body.Position,
                        0.5f);
                        var craftDetails = body.GetRoot().GetComponent<CraftDetails>();
                        if (craftDetails != null)
                        {
                            foreach (var resource in craftDetails.Resources)
                            {
                                var tag = resource.ResourceType;
                                for (int i = 0; i < resource.NumResources; i++)
                                {
                                    EntityFactory.CreateEntity<Body>(tag + " Resource",
                                        MathFunctions.RandVector3Box(body.GetBoundingBox()));
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            DefaultOnMouseOver(bodies);

            foreach (var body in bodies)
            {
                if (body.Tags.Contains("Moveable"))
                {
                    if (body.IsReserved)
                    {
                        Player.World.ShowTooltip("Can't move this " + body.Name + "\nIt is being used.");
                        continue;
                    }
                    Player.World.ShowTooltip("Left click to move this " + body.Name + "\nRight click to destroy it.");

                }
            }
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                return;
            }
            Player.VoxSelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            Player.BodySelector.Enabled = SelectedBody == null;
            Player.BodySelector.AllowRightClickSelection = true;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            
            if (SelectedBody != null)
            {
                var craftDetails = SelectedBody.GetRoot().GetComponent<CraftDetails>();

                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                if (voxelUnderMouse != prevVoxel && voxelUnderMouse.IsValid && voxelUnderMouse.IsEmpty)
                {
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_click_voxel, voxelUnderMouse.WorldPosition,
                        0.1f);
                    var offset = craftDetails != null ? CraftLibrary.CraftItems[craftDetails.CraftType].SpawnOffset : Vector3.Zero;
                    SelectedBody.LocalPosition = voxelUnderMouse.WorldPosition + Vector3.One * 0.5f + offset;
                    SelectedBody.HasMoved = true;
                    SelectedBody.UpdateTransform();
                    if (OverrideOrientation)
                    {
                        SelectedBody.Orient(CurrentOrientation);
                    }
                    else
                    {
                        SelectedBody.OrientToWalls();   
                    }
                    SelectedBody.UpdateBoundingBox();
                    SelectedBody.UpdateTransform();
                    SelectedBody.PropogateTransforms();
                    SelectedBody.UpdateBoundingBox();
                }

                var intersectsAnyOther =
                    Player.Faction.OwnedObjects.FirstOrDefault(
                        o => o != SelectedBody && o.Tags.Contains("Moveable") && o.GetRotatedBoundingBox().Intersects(SelectedBody.GetRotatedBoundingBox().Expand(-0.5f)));
                Drawer3D.DrawBox(SelectedBody.GetRotatedBoundingBox(), Color.White, 0.05f, false);

                bool intersectsWall = VoxelHelpers.EnumerateCoordinatesInBoundingBox(
                    SelectedBody.GetRotatedBoundingBox().Expand(-0.25f)).Any(
                        v =>
                        {
                            var tvh = new VoxelHandle(Player.VoxSelector.Chunks.ChunkData, v);
                            return tvh.IsValid && !tvh.IsEmpty;
                        });
                var tinter = SelectedBody.GetRoot().GetComponent<Tinter>();
                if (tinter != null)
                {
                    tinter.VertexColorTint = intersectsAnyOther == null && !intersectsWall ? Color.Green : Color.Red;
                }
                MouseState mouse = Mouse.GetState();
                if (mouse.LeftButton == ButtonState.Released && mouseDown)
                {
                    mouseDown = false;
                    if (intersectsAnyOther == null && !intersectsWall)
                    {
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.5f);
                        SelectedBody.UpdateTransform();
                        SelectedBody.PropogateTransforms();
                        SelectedBody.UpdateBoundingBox();
                        SelectedBody = null;
                        OverrideOrientation = false;
                        CurrentOrientation = 0;
                        if (tinter != null)
                            tinter.VertexColorTint = Color.White;
                    }
                    else if (!intersectsWall)
                    {
                        Player.World.ShowToolPopup("Can't move here: intersects " + intersectsAnyOther.Name);
                    }
                    else
                    {
                        Player.World.ShowToolPopup("Can't move here: intersects wall.");
                    }
                }
                else if (mouse.LeftButton == ButtonState.Pressed)
                {
                    mouseDown = true;
                }
                prevVoxel = voxelUnderMouse;

            }

            HandleOrientation();
        }

        private void HandleOrientation()
        {
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

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }
    }


    public class DeconstructObjectTool : PlayerTool
    {
        public DeconstructObjectTool()
        {
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            Player.VoxSelector.Clear();
        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            if (bodies.Count == 0)
                return;

            foreach (var body in bodies)
            {
                if (Player.Faction.OwnedObjects.Contains(body) && body.Tags.Any(tag => tag == "Moveable"))
                {
                    if (body.IsReserved)
                    {
                        Player.World.ShowToolPopup(string.Format("Can't move this {0}. It is being used.", body.Name));
                        continue;
                    }
                    body.Die();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, body.Position,
                    0.5f);
                }
            }
            
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            DefaultOnMouseOver(bodies);

            foreach (var body in bodies)
            {
                if (body.Tags.Contains("Moveable"))
                {
                    if (body.IsReserved)
                    {
                        Player.World.ShowTooltip("Can't destroy this this " + body.Name + "\nIt is being used.");
                        continue;
                    }
                    Player.World.ShowTooltip("Left click to destroy this " + body.Name);
                }
            }
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            Player.VoxSelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }
    }

}
