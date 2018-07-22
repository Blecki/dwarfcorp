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
    public class LayerStack
    {
        public class LayerEntry
        {
            public Layer Layer;
            public Palette Palette;
        }

        public Texture2D GetCompositeTexture()
        {
            return Composite;
        }

        public LayeredAnimationProxy ProxyAnimation(Animation animation)
        {
            var comp = animation as CompositeAnimation;
            var proxyAnim = new LayeredAnimationProxy(this)
            {
                Frames = comp == null ? animation.Frames : comp.CompositeFrames.Select(c => c.Cells[0].Tile).ToList(),
                Name = animation.Name,
                FrameHZ = 0.5f,
                Speeds = animation.Speeds,
                Tint = animation.Tint,
                Loops = animation.Loops
            };
            return proxyAnim;
        }

        private List<LayerEntry> Layers = new List<LayerEntry>();
        private bool CompositeValid = false;
        private Texture2D Composite;
        private MemoryTexture MemoryComposite;

        public void AddLayer(Layer Layer, Palette Palette)
        {
            if (Palette == null)
                Palette = LayerLibrary.BaseDwarfPalette;

            var existing = Layers.FirstOrDefault(l => l.Layer.Type == Layer.Type);
            if (existing == null)
                Layers.Add(new LayerEntry
                {
                    Layer = Layer,
                    Palette = Palette
                });
            else
            {
                existing.Layer = Layer;
                existing.Palette = Palette;
            }

            CompositeValid = false;
        }

        public void RemoveLayer(String Type)
        {
            Layers.RemoveAll(l => l.Layer.Type == Type);
            CompositeValid = false;
        }

        public void Update(GraphicsDevice Device)
        {
            if (!CompositeValid)
            {
                CompositeValid = true;
                Layers.Sort((a, b) => a.Layer.Precedence - b.Layer.Precedence);

                // Render the composite texture
                var maxSize = new Point(0, 0);
                foreach (var layer in Layers)
                {
                    maxSize.X = Math.Max(layer.Layer.CachedTexture.Width, maxSize.X);
                    maxSize.Y = Math.Max(layer.Layer.CachedTexture.Height, maxSize.Y);
                }

                if (maxSize.X == 0 || maxSize.Y == 0) return;

                if (MemoryComposite == null || MemoryComposite.Width != maxSize.X || MemoryComposite.Height != maxSize.Y)
                    MemoryComposite = new MemoryTexture(maxSize.X, maxSize.Y);

                TextureTool.ClearMemoryTexture(MemoryComposite);

                foreach (var layer in Layers)
                    TextureTool.Blit(layer.Layer.CachedTexture, layer.Palette.CachedPalette, MemoryComposite);

                Composite = TextureTool.Texture2DFromMemoryTexture(Device, MemoryComposite);
            }
        }

        internal object GetLayer(string v)
        {
            return Layers.Where(l => l.Layer.Type == v).FirstOrDefault();
        }
    }
}
