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


        public override void OnVoxelsSelected(List<Voxel> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, GameTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            PlayState.GUI.IsMouseVisible = true;

            if (PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Chop;
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

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

            List<Body> treesPickedByMouse = ComponentManager.FilterComponentsWithTag("Vegetation", bodies);

            foreach (Body tree in treesPickedByMouse)
            {
                if (!tree.IsVisible || tree.IsAboveCullPlane) continue;

                Drawer3D.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                if (button == InputManager.MouseButton.Left)
                {
                    if (!Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Add(tree);

                        foreach(CreatureAI creature in Player.Faction.SelectedMinions)
                        {
                            creature.Tasks.Add(new KillEntityTask(tree) { Priority = Task.PriorityType.Low});
                        }
                    }
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    if (Player.Faction.ChopDesignations.Contains(tree))
                    {
                        Player.Faction.ChopDesignations.Remove(tree);
                    }
                }
            }
        }
    }
}
