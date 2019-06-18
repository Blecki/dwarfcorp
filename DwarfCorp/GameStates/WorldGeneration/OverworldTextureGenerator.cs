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
        private const float PoliticalOverlayOpacity = 0.7f;

        public static void Generate(Overworld Overworld, bool ShowPolitics, Texture2D TerrainTexture, Texture2D FullTexture)
        {
            var colorData = new Color[Overworld.Width * Overworld.Height * 4 * 4];
            var stride = Overworld.Width * 4;

            Overworld.Map.CreateTexture(Overworld.Natives, 4, colorData, Overworld.GenerationSettings.SeaLevel);
            OverworldMap.Smooth(4, Overworld.Width, Overworld.Height, colorData);
            Overworld.Map.ShadeHeight(4, colorData);

            TerrainTexture.SetData(colorData);

            // Draw political boundaries
            if (ShowPolitics)
            {
                for (var x = 0; x < Overworld.Width; ++x)
                    for (var y = 0; y < Overworld.Height; ++y)
                    {
                        var thisCell = Overworld.ColonyCells.GetCellAt(x, y);
                        var rect = new Rectangle(x * 4, y * 4, 4, 4);

                        if (EnumerateNeighbors(new Point(x, y)).Select(p => Overworld.ColonyCells.GetCellAt(p.X, p.Y)).Any(c => c == null || !Object.ReferenceEquals(c.Faction, thisCell.Faction)))
                            FillSolidRectangle(rect, colorData, thisCell.Faction.PrimaryColor, stride);
                        else
                            FillPoliticalRectangle(rect, colorData, thisCell.Faction.PrimaryColor, stride);
                    }
            }

            foreach (var cell in Overworld.ColonyCells.EnumerateCells())
                DrawRectangle(new Rectangle(cell.Bounds.X * 4, cell.Bounds.Y * 4, cell.Bounds.Width * 4, cell.Bounds.Height * 4), colorData, Color.Black, stride);

            var spawnRect = new Rectangle((int)Overworld.InstanceSettings.Origin.X * 4, (int)Overworld.InstanceSettings.Origin.Y * 4,
                Overworld.InstanceSettings.Cell.Bounds.Width * 4, Overworld.InstanceSettings.Cell.Bounds.Height * 4);
            DrawRectangle(spawnRect, colorData, Color.Red, stride);

            FullTexture.SetData(colorData);
        }

        private static IEnumerable<Point> EnumerateNeighbors(Point Of)
        {
            yield return new Point(Of.X - 1, Of.Y);
            yield return new Point(Of.X - 1, Of.Y - 1);
            yield return new Point(Of.X,     Of.Y - 1);
            yield return new Point(Of.X + 1, Of.Y - 1);
            yield return new Point(Of.X + 1, Of.Y);
            yield return new Point(Of.X + 1, Of.Y + 1);
            yield return new Point(Of.X,     Of.Y + 1);
            yield return new Point(Of.X - 1, Of.Y + 1);
        }

        private static void DrawRectangle(Rectangle Rect, Color[] Into, Color Color, int Stride)
        {
            for (var x = 0; x < Rect.Width; ++x)
            {
                Into[(Rect.Y * Stride) + Rect.X + x] = Color;
                Into[((Rect.Y + Rect.Height - 1) * Stride) + Rect.X + x] = Color;
            }

            for (var y = 1; y < Rect.Height - 1; ++y)
            {
                Into[((Rect.Y + y) * Stride) + Rect.X] = Color;
                Into[((Rect.Y + y) * Stride) + Rect.X + Rect.Width - 1] = Color;
            }
        }

        private static void FillPoliticalRectangle(Rectangle R, Color[] Data, Color Color, int Stride)
        {
            if (StripeTexture == null)
                StripeTexture = TextureTool.MemoryTextureFromTexture2D(AssetManager.GetContentTexture("World\\stripes"));

            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                {
                    var tX = x % StripeTexture.Width;
                    var tY = y % StripeTexture.Height;
                    if (StripeTexture.Data[(tY * StripeTexture.Width) + tX].R != 0)
                        Data[(y * Stride) + x] = Blend(Data[(y * Stride) + x], Color, PoliticalOverlayOpacity);
                }
        }

        private static void FillSolidRectangle(Rectangle R, Color[] Data, Color Color, int Stride)
        {
            for (var x = R.Left; x < R.Right; ++x)
                for (var y = R.Top; y < R.Bottom; ++y)
                    Data[(y * Stride) + x] = Blend(Data[(y * Stride) + x], Color, PoliticalOverlayOpacity);
        }

        private static Color Blend(Color A, Color B, float Alpha)
        {
            var v = (A.ToVector3() * (1.0f - Alpha)) + (B.ToVector3() * Alpha);
            return new Color(v);
        }
    }
}