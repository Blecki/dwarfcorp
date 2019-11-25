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

        public void RemoveLayer(LayerLibrary.LayerType Type)
        {
            if (Layers.RemoveAll(l => l.Layer.Type == Type) != 0)
                CompositeValid = false;
        }

        public void Update(GraphicsDevice Device)
        {
            if (Composite == null || Composite.IsDisposed || Composite.GraphicsDevice == null || Composite.GraphicsDevice.IsDisposed)
            {
                CompositeValid = false;
            }

            if (!CompositeValid)
            {
                if (Composite != null && !Composite.IsDisposed)
                    Composite.Dispose();

                CompositeValid = true;
                Layers.Sort((a, b) => (int)a.Layer.Type - (int)b.Layer.Type);

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

        internal object GetLayer(LayerLibrary.LayerType v)
        {
            return Layers.Where(l => l.Layer.Type == v).FirstOrDefault();
        }
    }
}
