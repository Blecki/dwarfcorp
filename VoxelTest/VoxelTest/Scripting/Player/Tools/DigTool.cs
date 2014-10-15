using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// When using this tool, the player specifies that certain voxels should
    /// be mined.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DigTool : PlayerTool
    {
        public Color DigDesignationColor { get; set; }
        public Color UnreachableColor { get; set; }
        public float DigDesignationGlowRate { get; set; }

        public override void OnVoxelsSelected(List<Voxel> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                List<Task> assignments = new List<Task>();
                foreach (Voxel r in refs)
                {
                    if (r == null)
                    {
                        continue;
                    }

                    Voxel v = r;
                    if (v.IsEmpty)
                    {
                        continue;
                    }

                    if(!Player.Faction.IsDigDesignation(v))
                    {
                        BuildOrder d = new BuildOrder
                        {
                            Vox = r
                        };
                        Player.Faction.DigDesignations.Add(d);
                    }

                    assignments.Add(new KillVoxelTask(r));
                }

                TaskManager.AssignTasksGreedy(assignments, Faction.FilterMinionsWithCapability(Player.SelectedMinions, GameMaster.ToolMode.Dig), 5);
            }
            else
            {
                foreach (Voxel r in refs)
                {
                    if (r == null)
                    {
                        continue;
                    }
                    Voxel v = r;

                    if (v.IsEmpty)
                    {
                        continue;
                    }

                    if (Player.Faction.IsDigDesignation(v))
                    {
                        Player.Faction.DigDesignations.Remove(Player.Faction.GetDigDesignation(v));
                    }
                }
            }
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

            Player.VoxSelector.Enabled = true;
            PlayState.GUI.IsMouseVisible = true;

            if(PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Dig;
            }

            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            foreach (BuildOrder d in Player.Faction.DigDesignations)
            {
                Voxel v = d.Vox;

                BoundingBox box = v.GetBoundingBox();


                Color drawColor = DigDesignationColor;

                if (d.NumCreaturesAssigned == 0)
                {
                    drawColor = UnreachableColor;
                }

                drawColor.R = (byte)(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                drawColor.G = (byte)(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                drawColor.B = (byte)(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                Drawer3D.DrawBox(box, drawColor, 0.05f, true);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }
}
