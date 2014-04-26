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
    /// When using this tool, the player specifies that certain
    /// entities should be put into stockpiles.
    /// </summary>
    public class GatherTool : PlayerTool
    {

        public Color GatherDesignationColor { get; set; }
        public float GatherDesignationGlowRate { get; set; }

        public GatherTool()
        {

        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            List<Body> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag("Resource", bodies);

            foreach (Body resource in resourcesPickedByMouse.Where(resource => resource.IsActive && resource.IsVisible && resource.Parent == PlayState.ComponentManager.RootComponent))
            {
                Drawer3D.DrawBox(resource.BoundingBox, Color.LightGoldenrodYellow, 0.05f, true);

                if (button == InputManager.MouseButton.Left)
                    Player.Faction.AddGatherDesignation(resource);
                else
                {
                    if (!Player.Faction.GatherDesignations.Contains(resource))
                    {
                        continue;
                    }

                    Player.Faction.GatherDesignations.Remove(resource);
                }
            }
        }

        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, GameTime time)
        {
           
            if (Player.IsCameraRotationModeActive())
            {
                return;
            }
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            game.IsMouseVisible = true;


            /*
            MouseState mouseState = Mouse.GetState();
            Player.VoxSelector.Enabled = false;
            game.IsMouseVisible = true;

            List<Body> pickedByMouse = new List<Body>();
            PlayState.ComponentManager.GetBodiesUnderMouse(mouseState, Player.CameraController, PlayState.ChunkManager.Graphics.Viewport, pickedByMouse);

            List<Body> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag("Resource", pickedByMouse);

            foreach (Body resource in resourcesPickedByMouse.Where(resource => resource.IsActive && resource.IsVisible && resource.Parent == PlayState.ComponentManager.RootComponent))
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
             */
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
