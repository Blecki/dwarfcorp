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
    public class BuildZoneTool : PlayerTool
    {
        [ToolFactory("BuildZone")]
        private static PlayerTool _factory(GameMaster Master)
        {
            return new BuildZoneTool(Master);
        }

        public BuildZoneTool(GameMaster Master)
        {
            Player = Master;
        }

        private DestroyZoneTool DestroyZoneTool; // I should probably be fired for this.

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
                Player.World.PlayerFaction.RoomBuilder.VoxelsSelected(voxels, button);
            else
                DestroyZoneTool.OnVoxelsSelected(voxels, button);
        }

        public override void OnBegin()
        {
            Player.World.PlayerFaction.RoomBuilder.OnEnter();

            if (DestroyZoneTool == null)
                DestroyZoneTool = new DestroyZoneTool(Player);
        }

        public override void OnEnd()
        {
            Player.World.PlayerFaction.RoomBuilder.End();
            Player.VoxSelector.Clear();
            Player.World.PlayerFaction.RoomBuilder.OnExit();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Update(game, time);
            else
            {
                if (Player.IsCameraRotationModeActive())
                {
                    Player.VoxSelector.Enabled = false;
                    Player.World.SetMouse(null);
                    Player.BodySelector.Enabled = false;
                    return;
                }

                Player.VoxSelector.Enabled = true;
                Player.BodySelector.Enabled = false;
                Player.VoxSelector.DrawBox = true;
                Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

                if (Player.World.IsMouseOverGui)
                    Player.World.SetMouse(Player.World.MousePointer);
                else
                    Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));
            }
        }


        public override void Render2D(DwarfGame game, DwarfTime time)
        {

        }

        // Todo: Why is the graphics device passed in when we have a perfectly good global we're using instead?
        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Render3D(game, time);
            else
            {
                Player.World.PlayerFaction.RoomBuilder.Render(time, GameState.Game.GraphicsDevice);
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.OnVoxelsDragged(voxels, button);
            else
            {
                Player.World.PlayerFaction.RoomBuilder.OnVoxelsDragged(voxels, button);
            }
        }
    }
}
