// BuildRailTool.cs
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
    public class BuildRailTool : PlayerTool
    {
        public class RailDesignationInfo
        {
            public DecalType DecalType;
            public DecalOrientation Orientation;
        }

        public Shader Effect;
        private VoxelHandle Selected = VoxelHandle.InvalidHandle;
        public Rail.JunctionPattern Pattern;
        private bool RotateLeftPressed = false;
        private bool RotateRightPressed = false;
        private Rail.Orientations Orientation = Rail.Orientations.North;

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            var Faction = Player.Faction;

            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                        //TODO: Implement
                        //Use only the tile under the mouse for complex junctions

                        //List<Task> assignments = new List<Task>();
                        //var validRefs = voxels.Where(r => !Faction.Designations.IsVoxelDesignation(r, DesignationType.Put)
                        //    && Player.World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? r.IsEmpty : !r.IsEmpty).ToList();

                        //foreach (var r in validRefs)
                        //{

                        //Need new designation type for rail!

                        //    Faction.Designations.AddVoxelDesignation(r, DesignationType.Put,
                        //        (short)CurrentVoxelType);

                        //Need tasks for rail building!

                        //    assignments.Add(new BuildVoxelTask(r, VoxelLibrary.GetVoxelType(CurrentVoxelType).Name));
                        //}

                        ////TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(Player.World.Master.SelectedMinions, GameMaster.ToolMode.BuildZone));
                        //Player.TaskManager.AddTasks(assignments);
                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        //foreach (var r in voxels)
                        //{
                        //    Faction.Designations.RemoveVoxelDesignation(r, DesignationType.Put);
                        //}
                        break;
                    }
            }
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            Selected = VoxelHandle.InvalidHandle;
            Player.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            Player.BodySelector.Enabled = false;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (RotateLeftPressed && !leftKey)
                Orientation = Rail.Orientation.Rotate(Orientation, -1);
            else if (RotateRightPressed && !rightKey)
                Orientation = Rail.Orientation.Rotate(Orientation, 1);

            RotateLeftPressed = leftKey;
            RotateRightPressed = rightKey;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            //DepthStencilState state = graphics.DepthStencilState;
            //graphics.DepthStencilState = DepthStencilState.DepthRead;
            //Effect = Player.World.DefaultShader;

            //float t = (float)time.TotalGameTime.TotalSeconds;
            //float st = (float)Math.Sin(t * 4) * 0.5f + 0.5f;
            //Effect.MainTexture = Player.World.ChunkManager.ChunkData.Tilemap;
            //Effect.LightRampTint = Color.White;
            //Effect.VertexColorTint = new Color(0.1f, 0.9f, 1.0f, 0.5f * st + 0.45f);
            //Effect.SetTexturedTechnique();
            
            //if (Selected == null)
            //{
            //    Selected = new List<VoxelHandle>();
            //}

            //if (CurrentVoxelType == 0)
            //{
            //    Selected.Clear();
            //}

            //Effect.VertexColorTint = new Color(0.0f, 1.0f, 0.0f, 0.5f * st + 0.45f);
            //Vector3 offset = Player.World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? Vector3.Zero : Vector3.Up * 0.15f;

            //if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            //{
            //    foreach (var voxel in Selected)
            //    {
            //        Effect.World = Matrix.CreateTranslation(voxel.WorldPosition + offset);
            //        foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            //        {
            //            pass.Apply();
            //            VoxelLibrary.GetPrimitive(CurrentVoxelType).Render(graphics);
            //        }
            //    }
            //}

            //Effect.LightRampTint = Color.White;
            //Effect.VertexColorTint = Color.White;
            //Effect.World = Matrix.Identity;
            //graphics.DepthStencilState = state;
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                Player.World.ShowToolPopup("Release to build.");
            else
                Player.World.ShowToolPopup("Release to cancel.");

            Selected = Player.VoxSelector.VoxelUnderMouse;
        }
    }
}
