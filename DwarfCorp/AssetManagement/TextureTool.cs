// TextureManager.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
    public static class TextureTool
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
            {
                return null;
            }
            return new Palette(Source.Data);
        }

        public static Palette RawPaletteFromTexture2D(Texture2D Source)
        {
            return RawPaletteFromMemoryTexture(MemoryTextureFromTexture2D(Source));
        }

        public static IndexedDecomposition DecomposeTexture(MemoryTexture Source)
        {
            var r = new IndexedDecomposition();
            if (Source == null)
            {
                return r;
            }
            r.Palette = OptimizedPaletteFromMemoryTexture(Source);
            r.IndexedTexture = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
                r.IndexedTexture.Data[i] = (byte)r.Palette.IndexOf(Source.Data[i]);
            return r;
        }

        public static IndexedTexture DecomposeTexture(MemoryTexture Source, Palette Palette)
        {
            if (Source == null)
            {
                return null;
            }
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
            {
                return null;
            }
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

        public static Texture2D PaletteSwap(GraphicsDevice Device, Texture2D Source, Texture2D SourcePalette, Texture2D DestinationPalette)
        {
            var decomposedTexture = DecomposeTexture(MemoryTextureFromTexture2D(Source), RawPaletteFromTexture2D(SourcePalette));
            var composedTexture = ComposeTexture(decomposedTexture, RawPaletteFromTexture2D(DestinationPalette));
            return Texture2DFromMemoryTexture(Device, composedTexture);
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

        public static void Blit(MemoryTexture Source, MemoryTexture Onto)
        {
            if (Source == null || Onto == null)
            {
                return;
            }
            var width = Math.Min(Source.Width, Onto.Width);
            var height = Math.Min(Source.Height, Onto.Height);
            for (var y = 0; y < height; ++y)
            {
                var sourceIndex = Source.Index(0, y);
                var destIndex = Onto.Index(0, y);
                for (var x = 0; x < width; ++x)
                {
                    var sourcePixel = Source.Data[sourceIndex];
                    if (sourcePixel.A != 0)
                        Onto.Data[destIndex] = sourcePixel;
                    sourceIndex += 1;
                    destIndex += 1;
                }
            }
        }

        public static void Blit(IndexedTexture Source, Palette SourcePalette, MemoryTexture Destination)
        {
            if (Source == null || SourcePalette == null || Destination == null)
            {
                return;
            }

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
    }
}