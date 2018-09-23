// DwarfSelectorTool.cs
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
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class DwarfSelectorTool : PlayerTool
    {

        public Func<Body, bool> DrawSelectionRect = (b) => true;
        public DwarfSelectorTool(GameMaster master)
        {
           
            Player = master;
            InputManager.MouseClickedCallback += InputManager_MouseClickedCallback;
        }

        public override void Destroy()
        {
            InputManager.MouseClickedCallback -= InputManager_MouseClickedCallback;
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(button != InputManager.MouseButton.Right || Player.CurrentTool != this || KeyManager.RotationEnabled(Player.World.Camera))
            {
                return;
            }

            var mouseState = KeyManager.TrueMousePos;

            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(
                Player.World.ChunkManager.ChunkData,
                mouseState.X, mouseState.Y,
                Player.World.Camera, 
                GameState.Game.GraphicsDevice.Viewport,
                150.0f, 
                false,
                voxel => voxel.IsValid && (!voxel.IsEmpty || voxel.LiquidLevel > 0));

            if (!vox.IsValid)
                return;

            foreach(CreatureAI minion in Player.SelectedMinions)
            {
                if (minion.Creature.IsAsleep) continue;
                if(minion.CurrentTask != null)
                    minion.AssignTask(minion.CurrentTask);

                var above = VoxelHelpers.GetVoxelAbove(vox);
                minion.Blackboard.SetData("MoveTarget", above);

                minion.ChangeTask(new GoToNamedVoxelAct("MoveTarget", PlanAct.PlanType.Adjacent, minion).AsTask());
                minion.CurrentTask.AutoRetry = false;
                minion.CurrentTask.Priority = Task.PriorityType.Urgent;
            }
            OnConfirm(Player.SelectedMinions);

            if (Player.SelectedMinions.Count > 0)
                IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.DownArrow, vox.WorldPosition + Vector3.One * 0.5f, 0.5f, 2.0f, new Vector2(0, -50), Color.LightGreen);
        }



        public DwarfSelectorTool()
        {
            
        }
        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
          
        }

        bool IsNotSelectedDwarf(Body body)
        {
            if (body == null)
            {
                return true;
            }

            var dwarves = body.EnumerateAll().OfType<Creature>().ToList();

            if (dwarves.Count <= 0)
            {
                return false;
            }

            Creature dwarf = dwarves[0];
            return dwarf.Faction == Player.Faction && !Player.SelectedMinions.Contains(dwarf.AI);
        }

        bool IsDwarf(Body body)
        {
            if (body == null)
            {
                return false;
            }

            var dwarves = body.EnumerateAll().OfType<Creature>().ToList();

            if (dwarves.Count <= 0)
            {
                return false;
            }

            Creature dwarf = dwarves[0];
            return dwarf.Faction == Player.Faction;
        }

        protected void SelectDwarves(List<Body> bodies)
        {
            KeyboardState keyState = Keyboard.GetState();


            if(!keyState.IsKeyDown(Keys.LeftShift))
            {
                Player.SelectedMinions.Clear();
            }
            List<CreatureAI> newDwarves = new List<CreatureAI>();
            foreach(Body body in bodies)
            {
                if (IsNotSelectedDwarf(body))
                {
                    Player.SelectedMinions.Add(body.GetRoot().GetComponent<CreatureAI>());
                    newDwarves.Add(body.GetRoot().GetComponent<CreatureAI>());

                    Player.World.Tutorial("dwarf selected");
                }
            }
            OnConfirm(newDwarves);
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            switch(button)
            {
                case InputManager.MouseButton.Left:
                    SelectDwarves(bodies);
                    break;
            }
        }

        public static string GetMouseOverText(IEnumerable<Body> bodies)
        {
            StringBuilder sb = new StringBuilder();

            List<Body> bodyList = bodies.ToList();
            for (int i = 0; i < bodyList.Count; i++)
            {
                Creature dwarf = bodyList[i].GetComponent<Creature>();
                if (dwarf != null)
                {
                    sb.Append(dwarf.Stats.FullName + " (" + (dwarf.Stats.Title ?? dwarf.Stats.CurrentClass.Name) + ")");
                    if (dwarf.IsAsleep)
                    {
                        sb.Append(" UNCONSCIOUS ");
                    }

                    if (dwarf.Status.IsOnStrike)
                    {
                        sb.Append(" ON STRIKE");
                    }

                    if (i < bodyList.Count - 1)
                    {
                        sb.Append("\n");
                    }
                }
                else
                {
                    sb.Append(bodyList[i].GetDescription());
                }
            }
            return sb.ToString();
        }

        private List<Body> underMouse = null;

        public override void DefaultOnMouseOver(IEnumerable<Body> bodies)
        {
            Player.World.ShowTooltip(GetMouseOverText(bodies));
            underMouse = bodies.ToList();
        }

        public override void OnMouseOver(IEnumerable<Body> bodies)
        {
            DefaultOnMouseOver(bodies);
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = false;

            Player.World.SetMouse(Player.World.MousePointer);
        }

        public Rectangle GetScreenRect(BoundingBox Box, Camera Camera)
        {
            Vector3 ext = (Box.Max - Box.Min);
            Vector3 center = Box.Center();


            Vector3 p1 = Camera.Project(Box.Min);
            Vector3 p2 = Camera.Project(Box.Max);
            Vector3 p3 = Camera.Project(Box.Min + new Vector3(ext.X, 0, 0));
            Vector3 p4 = Camera.Project(Box.Min + new Vector3(0, ext.Y, 0));
            Vector3 p5 = Camera.Project(Box.Min + new Vector3(0, 0, ext.Z));
            Vector3 p6 = Camera.Project(Box.Min + new Vector3(ext.X, ext.Y, 0));


            Vector3 min = MathFunctions.Min(p1, p2, p3, p4, p5, p6);
            Vector3 max = MathFunctions.Max(p1, p2, p3, p4, p5, p6);

            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        public override void Render(DwarfGame game, DwarfTime time)
        {
            DwarfGame.SpriteBatch.Begin();

            foreach (var body in Player.BodySelector.CurrentBodies.Where(DrawSelectionRect))
            {
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, GetScreenRect(body.BoundingBox, Player.World.Camera), Color.White, 1.0f);
            }
            if (underMouse != null)
                foreach (var body in underMouse.Where(DrawSelectionRect))
                    Drawer2D.DrawRect(DwarfGame.SpriteBatch, GetScreenRect(body.BoundingBox, Player.World.Camera), Color.White, 1.0f);

            DwarfGame.SpriteBatch.End();
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
