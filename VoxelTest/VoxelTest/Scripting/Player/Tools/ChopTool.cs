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
    /// When using this tool, the player clicks on trees/bushes to specify that 
    /// they should be chopped down.
    /// </summary>
    public class ChopTool : PlayerTool
    {
        public Color ChopDesignationColor { get; set; }
        public float ChopDesignationGlowRate { get; set; }

        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, GameTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                game.IsMouseVisible = false;
                return;
            }

            MouseState mouseState = Mouse.GetState();
            Player.VoxSelector.Enabled = false;
            game.IsMouseVisible = true;

            List<LocatableComponent> pickedByMouse = new List<LocatableComponent>();
            PlayState.ComponentManager.GetComponentsUnderMouse(mouseState, Player.CameraController, PlayState.ChunkManager.Graphics.Viewport, pickedByMouse);

            List<LocatableComponent> treesPickedByMouse = ComponentManager.FilterComponentsWithTag("Tree", pickedByMouse);

            foreach (LocatableComponent tree in treesPickedByMouse)
            {
                Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Add(tree);
                    }
                }
                else if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Remove(tree);
                    }
                }
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {

            Color drawColor = ChopDesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * ChopDesignationGlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach(BoundingBox box in Player.Faction.ChopDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }
        }
    }
}
