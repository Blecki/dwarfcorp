using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Banner : Tinter
    {
        public CompanyInformation Logo;
        private Vector3 prevWindDirection;
        private float prevWindForce = 0.0f;
        private class BannerData
        {
            public VertexBuffer Mesh;
            public IndexBuffer Indices;
            public RenderTarget2D Texture;
            public ExtendedVertex[] Verts;
            public ushort[] Idx;
            public float SizeX;
            public float SizeY;
        }
        private static Dictionary<CompanyInformation, BannerData> banners = new Dictionary<CompanyInformation, BannerData>();

        public Banner()
        {
            LightsWithVoxels = true;
        }

        public Banner(ComponentManager Manager) :
            base(Manager, "Banner", Matrix.Identity, new Vector3(1, 1, 1), Vector3.Zero)
        {
            
        }

        private void GenerateData(GraphicsDevice device, float sizeX, float sizeY, float resolution)
        { 
            int numCellsX = (int)(sizeX/resolution);
            int numCellsY = (int) (sizeY/resolution);
            int numVerts = (numCellsX + 1)*(numCellsY + 1);
            int numIndices = numCellsX*numCellsY*6;
            float aspectRatio = sizeX / sizeY;
            const int height = 36;
            int width = (int) (aspectRatio*height);
            BannerData data = new BannerData()
            {
                Mesh = new VertexBuffer(device, ExtendedVertex.VertexDeclaration, numVerts, BufferUsage.None),
                Indices = new IndexBuffer(device, typeof(short), numIndices, BufferUsage.None),
                Texture = new RenderTarget2D(device, width, height),
                Verts = new ExtendedVertex[numVerts],
                SizeX = sizeX,
                SizeY = sizeY,
                Idx = new ushort[numIndices]
            };
            banners[Logo] = data;
            for (int i = 0, y = 0; y <= numCellsY; y++)
            {
                for (int x = 0; x <= numCellsX; x++, i++)
                {
                    data.Verts[i] = new ExtendedVertex(new Vector3(0, y * resolution - sizeY * 0.5f, 0), Color.White, Color.White, 
                        new Vector2((float)x / (float)numCellsX, 1.0f - (float)y / (float)numCellsY), new Vector4(0, 0, 1, 1));
                }
            }

            for (ushort ti = 0, vi = 0, y = 0; y < numCellsY; y++, vi++)
            {
                for (int x = 0; x < numCellsX; x++, ti += 6, vi++)
                {
                    data.Idx[ti] = vi;
                    data.Idx[ti + 3] = data.Idx[ti + 2] = (ushort)(vi + 1);
                    data.Idx[ti + 4] = data.Idx[ti + 1] = (ushort)(vi + numCellsX + 1);
                    data.Idx[ti + 5] = (ushort)(vi + numCellsX + 2);
                }
            }
            var oldView = device.Viewport;
            data.Mesh.SetData(data.Verts);
            data.Indices.SetData(data.Idx);
            device.SetRenderTarget(data.Texture);
            //device.Viewport = new Viewport(0, 0, width, height); // Must set viewport after target bound.
            device.Clear(new Color(Logo.LogoBackgroundColor * 0.5f + Logo.LogoSymbolColor * 0.5f));
            Texture2D logoBg = AssetManager.GetContentTexture("newgui/logo-bg");
            Texture2D logoFg = AssetManager.GetContentTexture("newgui/logo-fg");
            int bgIdx = Logo.LogoBackground.Tile;
            int bgX = (bgIdx%(logoBg.Width / 32)) * 32;
            int bgY = (bgIdx/(logoBg.Width / 32)) * 32; 
            int fgIdx = Logo.LogoSymbol.Tile;
            int fgX = (fgIdx % (logoFg.Width / 32)) * 32;
            int fgY = (fgIdx / (logoFg.Width / 32)) * 32;
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin,
                DepthStencilState.None, RasterizerState.CullNone);
            Drawer2D.DrawRect(DwarfGame.SpriteBatch, new Rectangle(1, 1, width - 1, height - 2), Color.Black, 2);
            DwarfGame.SpriteBatch.Draw(logoBg, new Rectangle(width / 2 - 16, height / 2 - 16, 32, 32), new Rectangle(bgX, bgY, 32, 32),  new Color(Logo.LogoBackgroundColor));
            DwarfGame.SpriteBatch.Draw(logoFg, new Rectangle(width / 2 - 16, height / 2 - 16, 32, 32), new Rectangle(fgX, fgY, 32, 32), new Color(Logo.LogoSymbolColor));
            DwarfGame.SpriteBatch.End();
            device.SetRenderTarget(null);
            device.Indices = null;
            device.SetVertexBuffer(null);
            //device.Viewport = oldView;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (!banners.ContainsKey(Logo))
                GenerateData(graphicsDevice, 1, 0.5f, 0.25f);

            var banner = banners[Logo];

            var oldWind = effect.WindDirection;
            var oldWindForce = effect.WindForce;
            
            if (Parent.HasValue(out var parent) && parent.GetRoot().GetComponent<Physics>().HasValue(out var phys))
            {
                Vector3 vel = -phys.Velocity;
                if (vel.LengthSquared() > 0.001f)
                    vel.Normalize();

                Vector3 newWind = vel;
                float newWindForce = Math.Max(phys.Velocity.LengthSquared() * 0.005f, 0.00001f);
                effect.WindForce = prevWindForce*0.9f + newWindForce*0.1f;
                effect.WindDirection = newWind * 0.1f + prevWindDirection * 0.9f;
                prevWindForce = effect.WindForce;
                prevWindDirection = effect.WindDirection;
            }

            if (banner.Mesh.IsDisposed || banner.Mesh.GraphicsDevice.IsDisposed || banner.Texture.IsDisposed || banner.Texture.IsContentLost)
                GenerateData(graphicsDevice, 1, 0.5f, 0.25f);
            if (banner.Mesh.IsDisposed || banner.Mesh.GraphicsDevice.IsDisposed || banner.Texture.IsDisposed || banner.Texture.IsContentLost)
                return;

            graphicsDevice.SetVertexBuffer(banner.Mesh);
            graphicsDevice.Indices = banner.Indices;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.TexturedFlag];
            Matrix world = Matrix.Identity;
            world.Translation = GlobalTransform.Translation;
            effect.World = world;
            effect.MainTexture = banner.Texture;
            effect.SelfIlluminationTexture = null;
            effect.LightRamp = LightRamp;
            effect.VertexColorTint = Color.White;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, banner.Verts.Length, 0, banner.Idx.Length / 3);
            }

            effect.SetTexturedTechnique();
            effect.WindDirection = oldWind;
            effect.WindForce = oldWindForce;
        }
    }
}
