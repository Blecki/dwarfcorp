// OrientedAnimation.cs
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
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.LayeredSprites
{
    public class LayerLibrary
    {
        private static List<Layer> Layers;
        private static List<Palette> Palettes;
        private static Palette _BaseDwarfPalette = null;

        public static Palette BaseDwarfPalette
        {
            get
            {
                if (_BaseDwarfPalette == null)
                {
                    var rawTexture = AssetManager.GetContentTexture(ContentPaths.dwarf_base_palette);
                    _BaseDwarfPalette = new Palette
                    {
                        CachedPalette = TextureTool.RawPaletteFromTexture2D(rawTexture),
                        Asset = ContentPaths.dwarf_base_palette
                    };
                }
                return _BaseDwarfPalette;
            }
        }

        private static void Initialize()
        {
            if (Layers != null) return;

            Layers = FileUtils.LoadJsonListFromMultipleSources<Layer>(ContentPaths.dwarf_layers, null, l => l.Type + "&" + l.Asset);

            foreach (var layer in Layers)
            {
                var rawTexture = AssetManager.GetContentTexture(layer.Asset);
                var memTexture = TextureTool.MemoryTextureFromTexture2D(rawTexture);
                layer.CachedTexture = TextureTool.DecomposeTexture(memTexture, BaseDwarfPalette.CachedPalette);
            }

            Palettes = FileUtils.LoadJsonListFromMultipleSources<Palette>(ContentPaths.dwarf_palettes, null, l => l.Asset);

            foreach (var palette in Palettes)
            {
                var rawTexture = AssetManager.GetContentTexture(palette.Asset);
                palette.CachedPalette = TextureTool.RawPaletteFromTexture2D(rawTexture);
            }
        }

        public static IEnumerable<Layer> EnumerateLayers(String LayerType)
        {
            Initialize();
            return Layers.Where(l => l.Type == LayerType);
        }

        public static IEnumerable<Palette> EnumeratePalettes()
        {
            return Palettes;
        }
    }
}
