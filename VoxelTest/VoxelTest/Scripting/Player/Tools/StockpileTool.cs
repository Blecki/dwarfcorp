﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Using this tool, the player specifies regions of voxels to be stockpiles.
    /// </summary>
    public class StockpileTool : PlayerTool
    {
        public Color DrawColor { get; set; }
        public float GlowRate { get; set; }

        public static BoundingBox ComputeBoundingBox(List<VoxelRef> voxels)
        {
            List<BoundingBox> boxes = voxels.Select(voxel => voxel.GetBoundingBox()).ToList();
            return MathFunctions.GetBoundingBox(boxes);
        }

        public override void OnVoxelsSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            Stockpile existingPile = null;

            BoundingBox boundBox = ComputeBoundingBox(refs);
            existingPile = Player.Faction.GetIntersectingStockpile(boundBox);

            foreach (Voxel v in refs.Where(r => r != null).Select(r => r.GetVoxel(false)).Where(v => v != null))
            {
                if (button == InputManager.MouseButton.Left)
                {
                    if (v.RampType != RampType.None)
                    {
                        continue;
                    }


                    if (Player.Faction.IsInStockpile(v))
                    {
                        continue;
                    }

                    Stockpile thisPile = Player.Faction.GetIntersectingStockpile(v);

                    if (existingPile == null)
                    {
                        existingPile = thisPile;
                    }

                    if (existingPile != null)
                    {
                        existingPile.AddVoxel(v.GetReference());
                    }
                    else
                    {
                        Stockpile newPile = new Stockpile("Stockpile " + Stockpile.NextID(), PlayState.ChunkManager);
                        newPile.AddVoxel(v.GetReference());

                        Player.Faction.Stockpiles.Add(newPile);
                        existingPile = newPile;
                    }
                }
                else
                {
                    if (v == null || !Player.Faction.IsInStockpile(v))
                    {
                        continue;
                    }

                    existingPile = Player.Faction.GetIntersectingStockpile(v);

                    if (existingPile == null)
                    {
                        continue;
                    }

                    existingPile.RemoveVoxel(v.GetReference());

                    if (existingPile.Storage.Count == 0)
                    {
                        existingPile.Destroy();
                    }
                }
            }
        }

        public override void Update(DwarfGame game, GameTime time)
        {
            if(Player.IsCameraRotationModeActive())
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
            Color drawColor = DrawColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach(Stockpile s in Player.Faction.Stockpiles)
            {
                BoundingBox box = s.GetBoundingBox();
                box.Max = new Vector3(box.Max.X, box.Max.Y + 0.05f, box.Max.Z);
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, false);
            }
        }
    }
}
