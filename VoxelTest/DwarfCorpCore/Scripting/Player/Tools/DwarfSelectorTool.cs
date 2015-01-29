using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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

                minion.CurrentTask = new GoToVoxelAct(vox, PlanAct.PlanType.Adjacent, minion).AsTask();
            }

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
                foreach(CreatureAI creature in Player.SelectedMinions)
                {
                    creature.Creature.SelectionCircle.IsVisible = false;
                }
                Player.SelectedMinions.Clear();


            }
            foreach(Body body in bodies)
            {
                List<Creature> dwarves = body.GetChildrenOfType<Creature>();

                if(dwarves.Count <= 0)
                {
                    continue;
                }

                Creature dwarf = dwarves[0];


                if (dwarf.Allies == Player.Faction.Alliance && !Player.SelectedMinions.Contains(dwarf.AI))
                {
                    Player.SelectedMinions.Add(dwarf.AI);
                }
            }
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
                Drawer2D.DrawAlignedText(DwarfGame.SpriteBatch, creature.Stats.FirstName + " " + creature.Stats.LastName, PlayState.GUI.SmallFont, Color.White, Drawer2D.Alignment.Right, new Rectangle(port.Width - 300, i * 24, 300, 24));
                i++;
            }

            DwarfGame.SpriteBatch.End();
             
        }
    }
}
