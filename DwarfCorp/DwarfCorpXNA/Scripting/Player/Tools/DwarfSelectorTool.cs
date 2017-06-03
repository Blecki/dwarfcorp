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
            if(button != InputManager.MouseButton.Right || Player.CurrentTool != this || KeyManager.RotationEnabled())
            {
                return;
            }

            Voxel vox = Player.World.ChunkManager.ChunkData.GetFirstVisibleBlockHitByMouse(Mouse.GetState(), 
                Player.World.Camera, GameState.Game.GraphicsDevice.Viewport, false, voxel => voxel != null && (!voxel.IsEmpty || voxel.WaterLevel > 0));
            if(vox == null)
            {
                return;
            }

            foreach(CreatureAI minion in Player.SelectedMinions)
            {
                if (minion.Creature.IsAsleep) continue;
                if(minion.CurrentTask != null)
                {
                    minion.Tasks.Add(minion.CurrentTask);
                    if (minion.CurrentTask.Script != null)
                    {
                        minion.CurrentAct.OnCanceled();
                        minion.CurrentTask.Script.Initialize();
                    }
                    minion.CurrentTask.SetupScript(minion.Creature);
                    minion.CurrentTask = null;
                }
                minion.Blackboard.SetData("MoveTarget", vox);
                minion.CurrentTask = new GoToVoxelAct("MoveTarget", PlanAct.PlanType.Adjacent, minion).AsTask();
                minion.CurrentTask.AutoRetry = false;
            }
            OnConfirm(Player.SelectedMinions);

            IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.DownArrow, vox.Position + Vector3.One * 0.5f, 0.5f, 2.0f, new Vector2(0, -50), Color.LightGreen);
        }



        public DwarfSelectorTool()
        {
            
        }
        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {
          
        }

        bool IsNotSelectedDwarf(Body body)
        {
            List<Creature> dwarves = body.GetChildrenOfType<Creature>();

            if (dwarves.Count <= 0)
            {
                return false;
            }

            Creature dwarf = dwarves[0];
            return dwarf.Faction == Player.Faction && !Player.SelectedMinions.Contains(dwarf.AI);
        }

        bool IsDwarf(Body body)
        {
            List<Creature> dwarves = body.GetChildrenOfType<Creature>();

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
                    Player.SelectedMinions.Add(body.GetComponent<CreatureAI>());
                    newDwarves.Add(body.GetComponent<CreatureAI>());

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

        public override void DefaultOnMouseOver(IEnumerable<Body> bodies)
        {
            StringBuilder sb = new StringBuilder();

            List<Body> bodyList = bodies.Where(IsDwarf).ToList();
            for (int i = 0; i < bodyList.Count; i++)
            {
                Dwarf dwarf = bodyList[i].GetComponent<Dwarf>();
                sb.Append(dwarf.Stats.FullName + " (" + dwarf.Stats.CurrentClass.Name + ")");
                if (i < bodyList.Count - 1)
                {
                    sb.Append("\n");
                }
            }
            Player.World.ShowTooltip(sb.ToString());
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

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            DwarfGame.SpriteBatch.Begin();
            Viewport port = GameState.Game.GraphicsDevice.Viewport;

            // Todo: Reimplement list of selected dwarves?
            //int i = 0;
            //foreach (CreatureAI creature in Player.SelectedMinions)
            //{
            //    Drawer2D.DrawAlignedText(DwarfGame.SpriteBatch, creature.Stats.FullName, Player.World.GUI.SmallFont, Color.White, Drawer2D.Alignment.Right, new Rectangle(port.Width - 300, 68 + i * 24, 300, 24));
            //    i++;
            //}

            foreach (Body body in Player.BodySelector.CurrentBodies)
            {
                if (IsDwarf(body))
                {
                    Drawer2D.DrawRect(DwarfGame.SpriteBatch, body.GetScreenRect(Player.World.Camera), Color.White, 1.0f);
                }
            }
             

            DwarfGame.SpriteBatch.End();

        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }
    }
}
