using System.Collections.Generic;
using System.Linq;
using LibNoise;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class OverworldTextureGenerator
    {
        private static MemoryTexture StripeTexture;

        public static void Generate(Overworld Overworld, bool ShowPolitics, Texture2D PreviewTexture)
        {
            var colorData = new Color[Overworld.Width * Overworld.Height * 4 * 4];

            Overworld.Map.CreateTexture(Overworld.Natives, 4, colorData, Overworld.GenerationSettings.SeaLevel);
            OverworldMap.Smooth(4, Overworld.Width, Overworld.Height, colorData);
            Overworld.Map.ShadeHeight(4, colorData);

            // Draw political boundaries
            if (ShowPolitics)
            {
                foreach (var cell in Overworld.ColonyCells.EnumerateCells())
                {
                    FillPoliticalRectangle(new Rectangle(cell.Bounds.Left * 4, cell.Bounds.Top * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, cell.Faction.PrimaryColor, Overworld);
                    foreach (var neighbor in Overworld.ColonyCells.EnumerateManhattanNeighbors(cell))
                    {
                        if (Object.ReferenceEquals(cell.Faction, neighbor.Faction))
                            continue;

                        // Todo: Get the inside corners!
                        if (neighbor.Bounds.Right <= cell.Bounds.Left)
                            DrawVerticalPoliticalEdge(cell.Bounds.Left, System.Math.Max(cell.Bounds.Top, neighbor.Bounds.Top), System.Math.Min(cell.Bounds.Bottom, neighbor.Bounds.Bottom), colorData, cell.Faction.PrimaryColor, Overworld);
                        if (neighbor.Bounds.Left >= cell.Bounds.Right)
                            DrawVerticalPoliticalEdge(cell.Bounds.Right - 2, System.Math.Max(cell.Bounds.Top, neighbor.Bounds.Top), System.Math.Min(cell.Bounds.Bottom, neighbor.Bounds.Bottom), colorData, cell.Faction.PrimaryColor, Overworld);
                        if (neighbor.Bounds.Bottom <= cell.Bounds.Top)
                            DrawHorizontalPoliticalEdge(System.Math.Max(cell.Bounds.Left, neighbor.Bounds.Left), System.Math.Min(cell.Bounds.Right, neighbor.Bounds.Right), cell.Bounds.Top, colorData, cell.Faction.PrimaryColor, Overworld);
                        if (neighbor.Bounds.Top >= cell.Bounds.Bottom)
                            DrawHorizontalPoliticalEdge(System.Math.Max(cell.Bounds.Left, neighbor.Bounds.Left), System.Math.Min(cell.Bounds.Right, neighbor.Bounds.Right), cell.Bounds.Bottom - 2, colorData, cell.Faction.PrimaryColor, Overworld);

                    }
                }
            }

            foreach (var cell in Overworld.ColonyCells.EnumerateCells())
                DrawRectangle(new Rectangle(cell.Bounds.X * 4, cell.Bounds.Y * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, Overworld.Width * 4, Color.Black);

            var spawnRect = new Rectangle((int)Overworld.InstanceSettings.Origin.X * 4, (int)Overworld.InstanceSettings.Origin.Y * 4,
                Overworld.InstanceSettings.Cell.Bounds.Width * 4, Overworld.InstanceSettings.Cell.Bounds.Height * 4);
            DrawRectangle(spawnRect, colorData, Overworld.Width * 4, Color.Red);

            PreviewTexture.SetData(colorData);
        }

        private static void DrawRectangle(Rectangle Rect, Color[] Into, int Width, Color Color)
        {
            for (var x = 0; x < Rect.Width; ++x)
            {
                Into[(Rect.Y * Width) + Rect.X + x] = Color;
                Into[((Rect.Y + Rect.Height - 1) * Width) + Rect.X + x] = Color;
            }

            for (var y = 1; y < Rect.Height - 1; ++y)
            {
                Into[((Rect.Y + y) * Width) + Rect.X] = Color;
                Into[((Rect.Y + y) * Width) + Rect.X + Rect.Width - 1] = Color;
            }
        }

        private static void DrawVerticalPoliticalEdge(int X, int MinY, int MaxY, Color[] Data, Color FactionColor, Overworld Overworld)
        {
            FillSolidRectangle(new Rectangle(X * 4, MinY * 4, 8, (MaxY - MinY) * 4), Data, FactionColor, Overworld);
        }

        private static void DrawHorizontalPoliticalEdge(int MinX, int MaxX, int Y, Color[] Data, Color FactionColor, Overworld Overworld)
        {
            FillSolidRectangle(new Rectangle(MinX * 4, Y * 4, (MaxX - MinX) * 4, 8), Data, FactionColor, Overworld);
        }

        private static void FillPoliticalRectangle(Rectangle R, Color[] Data, Color Color, Overworld Overworld)
        {
            if (StripeTexture == null)
                StripeTexture = TextureTool.MemoryTextureFromTexture2D(AssetManager.GetContentTexture("World\\stripes"));

            var stride = Overworld.Width * 4;
            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                {
                    var tX = x % StripeTexture.Width;
                    var tY = y % StripeTexture.Height;
                    if (StripeTexture.Data[(tY * StripeTexture.Width) + tX].R != 0)
                        Data[(y * stride) + x] = Color;
                }
        }

        private static void FillSolidRectangle(Rectangle R, Color[] Data, Color Color, Overworld Overworld)
        {
            var stride = Overworld.Width * 4;
            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                    Data[(y * stride) + x] = Color;
        }
    }
}