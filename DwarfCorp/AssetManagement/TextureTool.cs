using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class TextureTool
    {
        public static MemoryTexture MemoryTextureFromTexture2D(Texture2D Source)
        {
            if (Source == null || Source.IsDisposed || Source.GraphicsDevice.IsDisposed)
            {
                return null;
            }
            var r = new MemoryTexture(Source.Width, Source.Height);
            Source.GetData(r.Data);
            return r;
        }

        public static MemoryTexture MemoryTextureFromTexture2D(Texture2D Source, Rectangle SourceRect)
        {
            if (Source == null || Source.IsDisposed || Source.GraphicsDevice.IsDisposed)
            {
                return null;
            }
            var r = new MemoryTexture(SourceRect.Width, SourceRect.Height);
            Source.GetData(0, SourceRect, r.Data, 0, SourceRect.Width * SourceRect.Height);
            return r;
        }

        public static Texture2D Texture2DFromMemoryTexture(GraphicsDevice Device, MemoryTexture Source)
        {
            if (Source == null || Device.IsDisposed)
            {
                return null;
            }
            var r = new Texture2D(Device, Source.Width, Source.Height);
            r.SetData(Source.Data);
            return r;
        }

        public static Palette OptimizedPaletteFromMemoryTexture(MemoryTexture Source)
        {
            if (Source == null)
            {
                return null;
            }
            return new Palette(Source.Data.Distinct());
        }

        public static Palette RawPaletteFromMemoryTexture(MemoryTexture Source)
        {
            if (Source == null)
                return null;
            return new Palette(Source.Data);
        }

        public static Palette RawPaletteFromMemoryTextureRow(MemoryTexture Source, int Row)
        {
            if (Source == null)
                return null;
            return new Palette(Source.Data.Skip(Source.Index(0, Row)).Take(Source.Width));
        }

        public static Palette RawPaletteFromTexture2D(Texture2D Source)
        {
            return RawPaletteFromMemoryTexture(MemoryTextureFromTexture2D(Source));
        }

        public static IndexedDecomposition DecomposeTexture(MemoryTexture Source)
        {
            var r = new IndexedDecomposition();
            if (Source == null)
                return r;
            r.Palette = OptimizedPaletteFromMemoryTexture(Source);
            r.IndexedTexture = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
                r.IndexedTexture.Data[i] = (byte)r.Palette.IndexOf(Source.Data[i]);
            return r;
        }

        public static IndexedTexture DecomposeTexture(MemoryTexture Source, Palette Palette)
        {
            if (Source == null)
                return null;

            var r = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
            {
                var index = Palette.IndexOf(Source.Data[i]);
                if (index >= 0)
                    r.Data[i] = (byte)index;
                else
                    r.Data[i] = 0;
            }
            return r;
        }

        public static MemoryTexture ComposeTexture(IndexedTexture Source, Palette Palette)
        {
            if (Source == null)
                return null;
            var r = new MemoryTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
                r.Data[i] = Palette[Source.Data[i]];
            return r;
        }

        public static MemoryTexture MemoryTextureFromPalette(Palette Palette)
        {
            if (Palette == null)
            {
                return null;
            }
            var dim = (int)Math.Ceiling(Math.Sqrt(Palette.Count));
            var r = new MemoryTexture(dim, (int)Math.Ceiling((float)Palette.Count / dim));
            Palette.CopyTo(r.Data);
            return r;
        }

        public static Palette ExtractPaletteFromDirectoryRecursive(String Path)
        {
            var r = new Palette();
            foreach (var file in AssetManager.EnumerateAllFiles(Path))
            {
                var texture = AssetManager.RawLoadTexture(file);
                if (texture != null)
                    r.AddRange(OptimizedPaletteFromMemoryTexture(MemoryTextureFromTexture2D(texture)));
            }
            r = new Palette(r.Distinct());
            r.Sort((a, b) => (int)a.PackedValue - (int)b.PackedValue);
            return r;
        }

        public static void ClearMemoryTexture(MemoryTexture Texture)
        {
            if (Texture == null)
            {
                return;
            }

            for (var i = 0; i < Texture.Data.Length; ++i)
                Texture.Data[i] = Color.Transparent;
        }

        public static void Blit(IndexedTexture Source, Palette SourcePalette, MemoryTexture Destination)
        {
            if (Source == null || SourcePalette == null || Destination == null)
                return;

            var width = Math.Min(Source.Width, Destination.Width);
            var height = Math.Min(Source.Height, Destination.Height);

            for (var y = 0; y < height; ++y)
            {
                var sourceIndex = Source.Index(0, y);
                var destinationIndex = Destination.Index(0, y);
                var endSourceIndex = sourceIndex + width;

                while (sourceIndex < endSourceIndex)
                {
                    var sourcePixel = SourcePalette[Source.Data[sourceIndex]];
                    if (sourcePixel.A != 0)
                        Destination.Data[destinationIndex] = sourcePixel;

                    sourceIndex += 1;
                    destinationIndex += 1;
                }
            }
        }

        public static void Blit(MemoryTexture From, Rectangle SourceRect, MemoryTexture To, Point DestPoint)
        {
            if (From == null || To == null)
                return;

            for (var y = 0; y < SourceRect.Height; ++y)
            {
                if (y + DestPoint.Y < 0 || y + DestPoint.Y >= To.Height) continue; // This is ineffecient as all hell.
                if (y + SourceRect.Y < 0 || y + SourceRect.Y >= From.Height) continue;
                for (var x = 0; x < SourceRect.Width; ++x) // Actually can anyone even read this on the stream? All 1 of you lol
                {
                    if (x + DestPoint.X < 0 || x + DestPoint.X >= To.Width) continue;
                    if (x + SourceRect.X < 0 || x + SourceRect.X >= From.Width) continue;
                    To.Data[To.Index(x + DestPoint.X, y + DestPoint.Y)] = From.Data[From.Index(x + SourceRect.X, y + SourceRect.Y)];
                }
            }
        }

        public static void Blit(MemoryTexture From, Rectangle SourceRect, MemoryTexture To, Rectangle DestRect)
        {
            if (From == null || To == null)
                return;

            for (var y = 0; y < SourceRect.Height; ++y)
            {
                if (y + DestRect.Y < 0 || y + DestRect.Y >= To.Height || y + DestRect.Y >= DestRect.Bottom) continue; // This is ineffecient as all hell.
                if (y + SourceRect.Y < 0 || y + SourceRect.Y >= From.Height) continue;
                for (var x = 0; x < SourceRect.Width; ++x) // Actually can anyone even read this on the stream? All 1 of you lol
                {
                    if (x + DestRect.X < 0 || x + DestRect.X >= To.Width || x + DestRect.X >= DestRect.Right) continue;
                    if (x + SourceRect.X < 0 || x + SourceRect.X >= From.Width) continue;
                    To.Data[To.Index(x + DestRect.X, y + DestRect.Y)] = From.Data[From.Index(x + SourceRect.X, y + SourceRect.Y)];
                }
            }
        }

        public static MemoryTexture RotatedCopy(MemoryTexture Of)
        {
            var r = new MemoryTexture(Of.Width, Of.Height);
            for (var x = 0; x < Of.Width; ++x)
                for (var y = 0; y < Of.Height; ++y)
                    r.Data[r.Index(y, Of.Width - x - 1)] = Of.Data[Of.Index(x, y)];
            return r;
        }

        [ConsoleCommandHandler("PALETTE")]
        private static String DumpPalette(String Path)
        {
            var palette = TextureTool.ExtractPaletteFromDirectoryRecursive(Path);
            var paletteTexture = TextureTool.Texture2DFromMemoryTexture(DwarfGame.GuiSkin.Device, TextureTool.MemoryTextureFromPalette(palette));
            paletteTexture.SaveAsPng(File.OpenWrite("palette.png"), paletteTexture.Width, paletteTexture.Height);
            return "Dumped.";
        }
    }
}