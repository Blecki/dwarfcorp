using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class TiledInstanceGroup : InstanceGroup
    {
        private class InstanceTileSheet
        {
            public Gui.TileSheet Sheet;
            public Texture2D Texture;
        }

        private const int InstanceQueueSize = 1024;

        public InstanceRenderData RenderData;
        private TiledInstancedVertex[] Instances = new TiledInstancedVertex[InstanceQueueSize];
        private int InstanceCount = 0;
        private DynamicVertexBuffer InstanceBuffer = null;
        private Dictionary<String, InstanceTileSheet> Atlas = new Dictionary<string, InstanceTileSheet>();
        private List<Gui.TextureAtlas.SpriteAtlasEntry> AtlasEntries = null;
        private Rectangle AtlasBounds;
        private Texture2D AtlasTexture = null;
        private bool NeedsRendered = true;

        public TiledInstanceGroup()
        {
        }

        public override void Initialize()
        {
        }

        public Texture2D GetAtlasTexture()
        {
            return AtlasTexture;
        }

        public bool DoesInstanceSheetExist(String Name)
        {
            return Atlas.ContainsKey(Name);
        }

        private Vector4 GetTileBounds(NewInstanceData Instance)
        {
            if (Instance.AtlasCache != null)
                return Instance.AtlasCache.MapRectangleToUVBounds(Instance.SpriteBounds);

            if (Instance.TextureAsset == null)
                Instance.TextureAsset = "newgui/error";

            var exists = Atlas.TryGetValue(Instance.TextureAsset, out var sheet); // Need to give them dynamic names generated from asset and palette?
            if (!exists)
            {
                Texture2D tex = null;
                if (Instance.CachedSpriteSheet != null)
                    tex = Instance.CachedSpriteSheet.GetTexture();
                else
                    tex = AssetManager.GetContentTexture(Instance.TextureAsset);

                if (tex == null) return Vector4.Zero; // Actually should never happen.

                sheet = new InstanceTileSheet
                {
                    Sheet = new Gui.TileSheet(tex.Width, tex.Height, new Rectangle(0, 0, tex.Width, tex.Height), tex.Width, tex.Height, false),
                    Texture = tex
                };
                Atlas.Add(Instance.TextureAsset, sheet);

                RebuildAtlas();
                NeedsRendered = true;
            }
            Instance.AtlasCache = sheet.Sheet;
            return sheet.Sheet.MapRectangleToUVBounds(Instance.SpriteBounds);
        }

        private void RebuildAtlas()
        {
            AtlasEntries = Atlas.Select(s =>
            {
                return new Gui.TextureAtlas.SpriteAtlasEntry
                {
                    SourceDefinition = new Gui.TileSheetDefinition
                    {
                        Texture = s.Key,
                        Name = s.Key,
                        Type = Gui.TileSheetType.TileSheet,
                        TileHeight = s.Value.Sheet.TileHeight,
                        TileWidth = s.Value.Sheet.TileWidth
                    },
                    AtlasBounds = new Rectangle(0, 0, s.Value.Sheet.TileWidth, s.Value.Sheet.TileHeight),
                    SourceTexture = s.Value.Texture
                };
            }).ToList();

            AtlasBounds = Gui.TextureAtlas.Compiler.Compile(AtlasEntries);

            foreach (var texture in AtlasEntries)
            {
                var sheet = Atlas[texture.SourceDefinition.Name];
                sheet.Sheet.SourceRect = texture.AtlasBounds;
                sheet.Sheet.TextureWidth = AtlasBounds.Width;
                sheet.Sheet.TextureHeight = AtlasBounds.Height;
            }
        }

        public override void RenderInstance(NewInstanceData Instance, GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (Mode == InstanceRenderMode.SelectionBuffer && !RenderData.RenderInSelectionBuffer)
                return;
            if (InstanceCount >= InstanceQueueSize) return;

            Instances[InstanceCount] = new TiledInstancedVertex
            {
                Transform = Instance.Transform,
                LightRamp = Instance.LightRamp,
                SelectionBufferColor = Instance.SelectionBufferColor,
                VertexColorTint = Instance.VertexColorTint,
                TileBounds = GetTileBounds(Instance)
            };

            InstanceCount += 1;
            if (InstanceCount >= InstanceQueueSize)
                Flush(Device, Effect, Camera, Mode);
        }

        public override void Flush(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (InstanceCount == 0) return;

            if (NeedsRendered || (AtlasTexture != null && (AtlasTexture.IsDisposed || AtlasTexture.GraphicsDevice.IsDisposed)))
            {
                if (AtlasEntries == null)
                    RebuildAtlas();

                AtlasTexture = new Texture2D(Device, AtlasBounds.Width, AtlasBounds.Height);

                foreach (var texture in AtlasEntries)
                {
                    var realTexture = texture.SourceTexture;
                    if (realTexture == null || realTexture.IsDisposed || realTexture.GraphicsDevice.IsDisposed)
                    {
                        texture.SourceTexture = AssetManager.GetContentTexture(texture.SourceDefinition.Texture);
                        realTexture = texture.SourceTexture;
                    }

                    var textureData = new Color[realTexture.Width * realTexture.Height];
                    realTexture.GetData(textureData);

                    // Paste texture data into atlas.
                    AtlasTexture.SetData(0, texture.AtlasBounds, textureData, 0, realTexture.Width * realTexture.Height);
                }

                NeedsRendered = false;
            }

            if (InstanceBuffer == null || InstanceBuffer.IsDisposed || InstanceBuffer.IsContentLost)
                InstanceBuffer = new DynamicVertexBuffer(Device, TiledInstancedVertex.VertexDeclaration, InstanceQueueSize, BufferUsage.None);
            
            Device.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            if (Mode == InstanceRenderMode.Normal)
                Effect.SetTiledInstancedTechnique();
            else
                Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.SelectionBufferTiledInstanced];

            Effect.EnableWind = RenderData.EnableWind;
            Effect.EnableLighting = true;
            Effect.VertexColorTint = Color.White;

            if (RenderData.Model.VertexBuffer == null || RenderData.Model.IndexBuffer == null || 
                (RenderData.Model.VertexBuffer != null && RenderData.Model.VertexBuffer.IsContentLost) ||
                (RenderData.Model.IndexBuffer != null && RenderData.Model.IndexBuffer.IsContentLost))
                RenderData.Model.ResetBuffer(Device);

            Device.Indices = RenderData.Model.IndexBuffer;

            BlendState blendState = Device.BlendState;
            Device.BlendState = Mode == InstanceRenderMode.Normal ? BlendState.NonPremultiplied : BlendState.Opaque;

            Effect.MainTexture = AtlasTexture;
            Effect.LightRamp = Color.White;

            InstanceBuffer.SetData(Instances, 0, InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(new VertexBufferBinding(RenderData.Model.VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1));

            var ghostEnabled = Effect.GhostClippingEnabled;
            Effect.GhostClippingEnabled = RenderData.EnableGhostClipping && ghostEnabled;

            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    RenderData.Model.VertexCount, 0,
                    RenderData.Model.Indexes.Length / 3,
                    InstanceCount);
            }

            Effect.GhostClippingEnabled = ghostEnabled;
            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;
            Effect.MainTexture = null;

            InstanceCount = 0;
        }
    }
}