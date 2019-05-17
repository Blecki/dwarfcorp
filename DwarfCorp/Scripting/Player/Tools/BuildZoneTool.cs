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
        private static PlayerTool _factory(WorldManager World)
        {
            return new BuildZoneTool(World);
        }

        public BuildZoneTool(WorldManager World)
        {
            this.World = World;
        }

        private DestroyZoneTool DestroyZoneTool; // I should probably be fired for this.

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
                World.PlayerFaction.RoomBuilder.VoxelsSelected(voxels, button);
            else
                DestroyZoneTool.OnVoxelsSelected(voxels, button);
        }

        public override void OnBegin()
        {
            World.PlayerFaction.RoomBuilder.OnEnter();

            if (DestroyZoneTool == null)
                DestroyZoneTool = new DestroyZoneTool(World);
        }

        public override void OnEnd()
        {
            World.PlayerFaction.RoomBuilder.End();
            World.UserInterface.VoxSelector.Clear();
            World.PlayerFaction.RoomBuilder.OnExit();
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
                if (World.UserInterface.IsCameraRotationModeActive())
                {
                    World.UserInterface.VoxSelector.Enabled = false;
                    World.UserInterface.SetMouse(null);
                    World.UserInterface.BodySelector.Enabled = false;
                    return;
                }

                World.UserInterface.VoxSelector.Enabled = true;
                World.UserInterface.BodySelector.Enabled = false;
                World.UserInterface.VoxSelector.DrawBox = true;
                World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

                if (World.UserInterface.IsMouseOverGui)
                    World.UserInterface.SetMouse(World.UserInterface.MousePointer);
                else
                    World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));
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
                World.PlayerFaction.RoomBuilder.Render(time, GameState.Game.GraphicsDevice);
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
                World.PlayerFaction.RoomBuilder.OnVoxelsDragged(voxels, button);
            }
        }
    }
}
