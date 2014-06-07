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
                if(minion.CurrentTask != null)
                {
                    minion.Tasks.Add(minion.CurrentTask);
                    minion.CurrentTask = null;
                }

                minion.CurrentTask = new GoToVoxelAct(vox.GetReference(), PlanAct.PlanType.Adjacent, minion).AsTask();
            }

            IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.DownArrow, vox.Position + Vector3.One * 0.5f, 0.5f, 2.0f, new Vector2(0, -50), Color.LightGreen);
        }



        public DwarfSelectorTool()
        {
            
        }
        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
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
                List<Dwarf> dwarves = body.GetChildrenOfType<Dwarf>();

                if(dwarves.Count <= 0)
                {
                    continue;
                }

                Dwarf dwarf = dwarves[0];


                if (!Player.SelectedMinions.Contains(dwarf.AI))
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

        public override void Update(DwarfGame game, GameTime time)
        {
            PlayState.GUI.IsMouseVisible = true;
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = false;

            PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            /*
            foreach (CreatureAI creature in Player.SelectedMinions)
            {
                Drawer2D.DrawZAlignedRect(creature.Position + Vector3.Down * 0.5f, 0.25f, 0.25f, 1, new Color(100, 100, 100, 100));
                //Drawer2D.DrawRect(creature.AI.Position, new Rectangle(0, 0, 64, 64), Color.Transparent, new Color(100, 100, 100, 100), 1);
            }
             */
        }
    }
}
