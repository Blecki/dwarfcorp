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
            Player.BodySelector.Enabled = false;
            Player.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
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
