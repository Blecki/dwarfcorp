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
    /// Using this tool, the player can specify certain voxels to be guarded.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GuardTool : PlayerTool
    {

        public Color GuardDesignationColor { get; set; }
        public float GuardDesignationGlowRate { get; set; }
        public Color UnreachableColor { get; set; }

        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {
            List<Task> assignedTasks = new List<Task>();


            foreach (Voxel v in from r in voxels
                                where r != null
                                select r.GetVoxel(false))
            {
                if (button == InputManager.MouseButton.Left)
                {
                    if (v == null || Player.Faction.IsGuardDesignation(v))
                    {
                        continue;
                    }

                    Designation d = new Designation
                    {
                        Vox = v.GetReference()
                    };

                    Player.Faction.GuardDesignations.Add(d);
                    assignedTasks.Add(new GuardVoxelTask(v.GetReference()));
                }
                else
                {
                    if (v == null || !Player.Faction.IsGuardDesignation(v))
                    {
                        continue;
                    }

                    Player.Faction.GuardDesignations.Remove(Player.Faction.GetGuardDesignation(v));

                }
            }

            TaskManager.AssignTasks(assignedTasks, Player.SelectedMinions);
        }

        public override void Update(DwarfGame game, GameTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                PlayState.GUI.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            PlayState.GUI.IsMouseVisible = true;
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

            if (PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Guard;
            }
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            foreach (Designation d in Player.Faction.GuardDesignations)
            {
                VoxelRef v = d.Vox;

                if (v == null)
                {
                    continue;
                }

                BoundingBox box = v.GetBoundingBox();


                Color drawColor = GuardDesignationColor;

                if (d.NumCreaturesAssigned == 0)
                {
                    drawColor = UnreachableColor;
                }

                drawColor.R = (byte)(Math.Min(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                drawColor.G = (byte)(Math.Min(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                drawColor.B = (byte)(Math.Min(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                Drawer3D.DrawBox(box, drawColor, 0.05f, true);
            }
        }

        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            
        }
    }
}
