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

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }


        void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(button != InputManager.MouseButton.Right || Player.CurrentTool != this)
            {
                return;
            }

            Voxel vox = PlayState.ChunkManager.ChunkData.GetFirstVisibleBlockHitByMouse(Mouse.GetState(), PlayState.Camera, GameState.Game.GraphicsDevice.Viewport);
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
                List<Creature> dwarves = body.GetChildrenOfType<Creature>();

                if(dwarves.Count <= 0)
                {
                    continue;
                }

                Creature dwarf = dwarves[0];


                if (dwarf.Allies == Player.Faction.Name && !Player.SelectedMinions.Contains(dwarf.AI))
                {
                    Player.SelectedMinions.Add(dwarf.AI);
                    newDwarves.Add(dwarf.AI);
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

        public override void Update(DwarfGame game, DwarfTime time)
        {
            PlayState.GUI.IsMouseVisible = true;
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = false;

            PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, DwarfTime time)
        {
            DwarfGame.SpriteBatch.Begin();
            int i = 0;
            Viewport port = GameState.Game.GraphicsDevice.Viewport;
            foreach (CreatureAI creature in Player.SelectedMinions)
            {
                Drawer2D.DrawAlignedText(DwarfGame.SpriteBatch, creature.Stats.FullName, PlayState.GUI.SmallFont, Color.White, Drawer2D.Alignment.Right, new Rectangle(port.Width - 300, 68 + i * 24, 300, 24));
                i++;
            }

            DwarfGame.SpriteBatch.End();
             
        }

        public override void OnVoxelsDragged(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }
    }
}
