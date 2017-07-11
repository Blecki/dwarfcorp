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
        public MoveObjectTool()
        {

        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

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
                            SelectedBody = body;
                            break;
                        }
                    }
                    else if (Player.Faction.OwnedObjects.Contains(body) && body.Tags.Any(tag => tag == "Moveable") && !body.IsReserved)
                    {
                        body.Delete();
                        if (CraftLibrary.CraftItems.ContainsKey(body.Name))
                        {
                            var item = CraftLibrary.CraftItems[body.Name];
                            foreach (var resource in item.RequiredResources)
                            {
                                var tag = resource.ResourceType;
                                var resourcesWithTag = ResourceLibrary.GetLeastValuableWithTag(tag);
                                for (int i = 0; i < resource.NumResources; i++)
                                {
                                    EntityFactory.CreateEntity<Body>(resourcesWithTag.ResourceName + " Resource",
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
                var voxelUnderMouse = Player.VoxSelector.VoxelUnderMouse;
                if (voxelUnderMouse != null && voxelUnderMouse.IsEmpty)
                {
                    SelectedBody.LocalPosition = voxelUnderMouse.Position + Vector3.One * 0.5f;
                    SelectedBody.HasMoved = true;
                    SelectedBody.UpdateTransformsRecursive(SelectedBody.Parent as Body);
                }

                bool intersectsAnyOther =
                    Player.Faction.OwnedObjects.Any(
                        o => o != SelectedBody && o.GetBoundingBox().Intersects(SelectedBody.GetBoundingBox()));

                var tinter = SelectedBody.GetComponent<Tinter>();
                if (tinter != null)
                {
                    tinter.VertexColorTint = !intersectsAnyOther ? Color.Green : Color.Red;
                }
                MouseState mouse = Mouse.GetState();
                SelectedBody.OrientToWalls();
                if (mouse.LeftButton == ButtonState.Released && mouseDown)
                {
                    mouseDown = false;
                    if (!intersectsAnyOther)
                    {
                        SelectedBody = null;
                        if (tinter != null)
                            tinter.VertexColorTint = Color.White;
                    }
                    else
                    {
                        Player.World.ShowToolPopup("Can't move here, it intersects something else.");
                    }
                }
                else if (mouse.LeftButton == ButtonState.Pressed)
                {
                    mouseDown = true;
                }


            }

        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {

        }
    }
}
