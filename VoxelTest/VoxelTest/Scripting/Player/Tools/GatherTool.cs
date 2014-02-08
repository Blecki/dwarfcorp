using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            List<LocatableComponent> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag("Resource", pickedByMouse);

            foreach (LocatableComponent resource in resourcesPickedByMouse.Where(resource => resource.IsActive && resource.IsVisible && resource.Parent == PlayState.ComponentManager.RootComponent && !Player.Faction.IsInStockpile(resource)))
            {
                Drawer3D.DrawBox(resource.BoundingBox, Color.LightGoldenrodYellow, 0.05f, true);
                
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    Player.Faction.AddGatherDesignation(resource);
                }
                else if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (!Player.Faction.GatherDesignations.Contains(resource))
                    {
                        continue;
                    }

                    Player.Faction.GatherDesignations.Remove(resource);
                }
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            Color drawColor = GatherDesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GatherDesignationGlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.GatherDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }
        }
    }
}
