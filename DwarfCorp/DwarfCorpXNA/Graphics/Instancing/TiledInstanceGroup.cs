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
        private const int InstanceQueueSize = 64;

        public InstanceRenderData RenderData;
        private TiledInstancedVertex[] Instances = new TiledInstancedVertex[InstanceQueueSize];
        private int InstanceCount = 0;
        private DynamicVertexBuffer InstanceBuffer = null;
        private Dictionary<String, Gui.ITileSheet> Atlas = new Dictionary<string, Gui.ITileSheet>();
        private List<Gui.JsonTileSheet> TileSheets = new List<Gui.JsonTileSheet>();
        private Gui.TextureAtlas.Atlas RawAtlas = null;
        private Texture2D AtlasTexture = null;
        private bool NeedsRendered = true;

        public TiledInstanceGroup()
        {
        }

        public override void Initialize()
        {
            RenderData.Model = PrimitiveLibrary.Primitives[RenderData.PrimitiveName];
        }

        private Vector4 GetTileBounds(String Texture, Rectangle Tile)
        {
            Gui.ITileSheet sheet = null;

            if (!Atlas.TryGetValue(Texture, out sheet))
            {
                var tex = AssetManager.GetContentTexture(Texture);
                if (tex == null) return Vector4.Zero; // Actually should never happen.

                // Add sheet to set and recompile atlas.
                TileSheets.Add(new Gui.JsonTileSheet
                {
                    Texture = Texture,
                    Name = Texture,
                    TileWidth = tex.Width,
                    TileHeight = tex.Height
                });

                RebuildAtlas();

                sheet = Atlas[Texture];

                NeedsRendered = true;
            }

            return sheet.MapRectangleToUVBounds(Tile);
        }

        private void RebuildAtlas()
        {
            RawAtlas = Gui.TextureAtlas.Compiler.Compile(TileSheets.Select(s =>
            {
                var realTexture = AssetManager.GetContentTexture(s.Texture);
                return new Gui.TextureAtlas.Entry
                {
                    Sheet = s,
                    Rect = new Rectangle(0, 0, realTexture.Width, realTexture.Height),
                    RealTexture = realTexture
                };
            }).ToList());

            Atlas = new Dictionary<string, Gui.ITileSheet>();

            foreach (var texture in RawAtlas.Textures)
                Atlas[texture.Sheet.Name] = new Gui.TileSheet(RawAtlas.Dimensions.Width,
                    RawAtlas.Dimensions.Height, texture.Rect, texture.Sheet.TileWidth, texture.Sheet.TileHeight, texture.Sheet.RepeatWhenUsedAsBorder);
        }

        public override void RenderInstance(NewInstanceData Instance, GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (Mode == InstanceRenderMode.SelectionBuffer && !RenderData.RenderInSelectionBuffer)
                return;
            if (InstanceCount >= InstanceQueueSize) return;

            Instances[InstanceCount] = new TiledInstancedVertex
            {
                Transform = Instance.Transform,
                Color = Instance.Color,
                SelectionBufferColor = Instance.SelectionBufferColor,
                TileBounds = GetTileBounds(Instance.TextureAsset, Instance.SpriteBounds)
            };

            InstanceCount += 1;
            if (InstanceCount >= InstanceQueueSize)
                Flush(Device, Effect, Camera, Mode);
        }

        public override void Flush(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (InstanceCount == 0) return;

            if (NeedsRendered || (AtlasTexture != null && AtlasTexture.IsDisposed))
            {
                if (RawAtlas == null || RawAtlas.Textures.Count == 0)
                {
                    RebuildAtlas();
                    if (RawAtlas == null || RawAtlas.Textures.Count == 0)
                    {
                        // WTF.
                        InstanceCount = 0;
                        return;
                    }
                }

                AtlasTexture = new Texture2D(Device, RawAtlas.Dimensions.Width, RawAtlas.Dimensions.Height);

                foreach (var texture in RawAtlas.Textures)
                {
                    var realTexture = texture.RealTexture;
                    var textureData = new Color[realTexture.Width * realTexture.Height];
                    realTexture.GetData(textureData);

                    // Paste texture data into atlas.
                    AtlasTexture.SetData(0, texture.Rect, textureData, 0, realTexture.Width * realTexture.Height);
                }

                NeedsRendered = false;
            }

            if (InstanceBuffer == null)
                InstanceBuffer = new DynamicVertexBuffer(Device, TiledInstancedVertex.VertexDeclaration, InstanceQueueSize, BufferUsage.None);
            
            Device.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            if (Mode == InstanceRenderMode.Normal)
                Effect.SetTiledInstancedTechnique();
            else
                Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.SelectionBufferTiledInstanced];

            Effect.EnableWind = RenderData.EnableWind;
            Effect.EnableLighting = true;
            Effect.VertexColorTint = Color.White;

            if (RenderData.Model.VertexBuffer == null || RenderData.Model.IndexBuffer == null)
                RenderData.Model.ResetBuffer(Device);

            Device.Indices = RenderData.Model.IndexBuffer;

            BlendState blendState = Device.BlendState;
            Device.BlendState = Mode == InstanceRenderMode.Normal ? BlendState.NonPremultiplied : BlendState.Opaque;

            Effect.MainTexture = AtlasTexture;
            Effect.LightRampTint = Color.White;

            InstanceBuffer.SetData(Instances, 0, InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(RenderData.Model.VertexBuffer, new VertexBufferBinding(InstanceBuffer, 0, 1));

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

            InstanceCount = 0;
        }
    }
}