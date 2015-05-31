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
    /// When using this tool, the player specifies that certain
    /// entities should be put into stockpiles.
    /// </summary>
    public class GatherTool : PlayerTool
    {

        public Color GatherDesignationColor { get; set; }
        public float GatherDesignationGlowRate { get; set; }

        public GatherTool()
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
            List<Body> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag("Resource", bodies);
            List<Task> assignments = new List<Task>();
            foreach(Body resource in resourcesPickedByMouse.Where(resource => resource.IsActive && resource.IsVisible && resource.Parent == PlayState.ComponentManager.RootComponent))
            {
                if (!resource.IsVisible || resource.IsAboveCullPlane) continue;
                Drawer3D.DrawBox(resource.BoundingBox, Color.LightGoldenrodYellow, 0.05f, true);

                if(button == InputManager.MouseButton.Left)
                {
                    Player.Faction.AddGatherDesignation(resource);

                    assignments.Add(new GatherItemTask(resource));
                }
                else
                {
                    if(!Player.Faction.GatherDesignations.Contains(resource))
                    {
                        continue;
                    }

                    Player.Faction.GatherDesignations.Remove(resource);
                }
            }

            TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(PlayState.Master.SelectedMinions, GameMaster.ToolMode.Gather));
        }

        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
           
            if (Player.IsCameraRotationModeActive())
            {
                return;
            }
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;
            PlayState.GUI.IsMouseVisible = true;

            if (PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Gather;
            }


        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            /*
            Color drawColor = GatherDesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GatherDesignationGlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.GatherDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }
             */

        }
    }
}
