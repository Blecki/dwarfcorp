using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{ 
    public class LayerStack
    {
        public static int CompositesRebuilt = 0;

        public class LayerEntry
        {
            public Layer Layer;
            public Palette Palette;
        }

        public Texture2D GetCompositeTexture()
        {
            return Composite;
        }

        private List<LayerEntry> Layers = new List<LayerEntry>();
        private bool CompositeValid = false;
        private Texture2D Composite;

        public void AddLayer(Layer Layer, Palette Palette)
        {
            if (Palette == null)
                Palette = LayerLibrary.BasePalette;

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
            if (Layers.RemoveAll(l => l.Layer.Type == Type) != 0)
                CompositeValid = false;
        }

        private int GetLayerPrecedence(Layer Layer)
        {
            if (LayerLibrary.GetLayerType(Layer.Type).HasValue(out var type))
                return type.Precedence;
            return 0;
        }

        public void Update(GraphicsDevice Device)
        {
            if (Composite == null || Composite.IsDisposed || Composite.GraphicsDevice == null || Composite.GraphicsDevice.IsDisposed)
            {
                CompositeValid = false;
            }

            if (!CompositeValid)
            {
                CompositesRebuilt += 1;

                CompositeValid = true;
                Layers.Sort((a, b) => GetLayerPrecedence(a.Layer) - GetLayerPrecedence(b.Layer));

                // Render the composite texture
                var maxSize = new Point(0, 0);
                foreach (var layer in Layers)
                {
                    maxSize.X = Math.Max(layer.Layer.CachedTexture.Width, maxSize.X);
                    maxSize.Y = Math.Max(layer.Layer.CachedTexture.Height, maxSize.Y);
                }

                if (maxSize.X == 0 || maxSize.Y == 0) return;

                var memoryComposite = new MemoryTexture(maxSize.X, maxSize.Y);

                foreach (var layer in Layers)
                    TextureTool.Blit(layer.Layer.CachedTexture, layer.Palette.CachedPalette, memoryComposite);

                if (Composite == null || Composite.IsDisposed || Composite.Width != memoryComposite.Width || Composite.Height != memoryComposite.Height)
                {
                    if (Composite != null && !Composite.IsDisposed)
                        Composite.Dispose();

                    Composite = new Texture2D(Device, memoryComposite.Width, memoryComposite.Height);
                }

                TextureTool.CopyMemoryTextureToTexture2D(memoryComposite, Composite);
            }
        }
    }
}
