using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override void OnVoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                foreach (VoxelRef r in refs)
                {
                    if (r == null)
                    {
                        continue;
                    }

                    Voxel v = r.GetVoxel(false);
                    if (v == null || Player.Faction.IsDigDesignation(v))
                    {
                        continue;
                    }

                    Designation d = new Designation
                    {
                        Vox = r
                    };
                    Player.Faction.DigDesignations.Add(d);
                }
            }
            else
            {
                foreach (VoxelRef r in refs)
                {
                    if (r == null)
                    {
                        continue;
                    }
                    Voxel v = r.GetVoxel(false);

                    if (v == null)
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
                game.IsMouseVisible = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            game.IsMouseVisible = true;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            foreach (Designation d in Player.Faction.DigDesignations)
            {
                VoxelRef v = d.Vox;

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
    }
}
