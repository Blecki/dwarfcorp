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
    /// When using this tool, the player clicks on creatures to specify that 
    /// they should be killed
    /// </summary>
    public class AttackTool : PlayerTool
    {
        public Color DesignationColor { get; set; }
        public float GlowRate { get; set; }

        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
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
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Attack;
            }


        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {

            Color drawColor = DesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.AttackDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {

            foreach (Body other in bodies)
            {
                Creature creature = other.GetChildrenOfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                if (Alliance.GetRelationship(creature.Allies, Player.Faction.Alliance) == Relationship.Loves)
                {
                    continue;
                }

                Drawer3D.DrawBox(other.BoundingBox, DesignationColor, 0.1f, false);
                if (button == InputManager.MouseButton.Left)
                {
                    if (!Player.Faction.AttackDesignations.Contains(other))
                    {
                        Player.Faction.AttackDesignations.Add(other);

                        foreach (CreatureAI minion in Player.Faction.SelectedMinions)
                        {
                            minion.Tasks.Add(new KillEntityTask(other));
                        }
                    }
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    if (Player.Faction.AttackDesignations.Contains(other))
                    {
                        Player.Faction.AttackDesignations.Remove(other);
                    }
                }
            }
        }
    }
}
