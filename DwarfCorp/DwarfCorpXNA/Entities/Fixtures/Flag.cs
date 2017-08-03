// Flag.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Flag : Body
    {
        public Flag()
        {

        }

        public Flag(ComponentManager Manager, Vector3 position, CompanyInformation logo) :
            base(Manager, "Flag", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            List<Point> frames = new List<Point>
            {
                new Point(0, 2)
            };
            Animation lampAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "Flag", 32, 32, frames, true, Color.White, 5.0f + MathFunctions.Rand(), 1f, 1.0f, false);

            Sprite sprite = AddChild(new Sprite(Manager, "sprite", Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.YAxis
            }) as Sprite;
            sprite.AddAnimation(lampAnimation);


            AddChild(new Banner(Manager)
            {
                Logo = logo
            });
            Tags.Add("Flag");

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                Manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(Manager, Manager.World.ChunkManager,
                    voxelUnder));

            CollisionType = CollisionManager.CollisionType.Static;
        }
    }

    [JsonObject(IsReference = true)]
    public class Banner : Tinter, IRenderableComponent
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
            base(Manager, "Banner", Matrix.Identity, new Vector3(1, 1, 1), Vector3.Zero, false)
        {
            
        }

        private void GenerateData(GraphicsDevice device, float sizeX, float sizeY, float resolution)
        {
            if (banners.ContainsKey(Logo))
            {
                return;
            }

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
            device.Viewport = new Viewport(0, 0, width, height); // Must set viewport after target bound.
            device.Clear(new Color(Logo.LogoBackgroundColor * 0.5f + Logo.LogoSymbolColor * 0.5f));
            Texture2D logoBg = TextureManager.GetTexture("newgui/logo-bg");
            Texture2D logoFg = TextureManager.GetTexture("newgui/logo-fg");
            int bgIdx = Logo.LogoBackground.Tile;
            int bgX = (bgIdx%(logoBg.Width / 32)) * 32;
            int bgY = (bgIdx/(logoBg.Width / 32)) * 32; 
            int fgIdx = Logo.LogoSymbol.Tile;
            int fgX = (fgIdx % (logoFg.Width / 32)) * 32;
            int fgY = (fgIdx / (logoFg.Width / 32)) * 32;
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            Drawer2D.DrawRect(DwarfGame.SpriteBatch, new Rectangle(1, 1, width - 1, height - 2), Color.Black, 2);
            DwarfGame.SpriteBatch.Draw(logoBg, new Rectangle(width / 2 - 16, height / 2 - 16, 32, 32), new Rectangle(bgX, bgY, 32, 32),  new Color(Logo.LogoBackgroundColor));
            DwarfGame.SpriteBatch.Draw(logoFg, new Rectangle(width / 2 - 16, height / 2 - 16, 32, 32), new Rectangle(fgX, fgY, 32, 32), new Color(Logo.LogoSymbolColor));
            DwarfGame.SpriteBatch.End();
            device.SetRenderTarget(null);
            device.Indices = null;
            device.SetVertexBuffer(null);
            device.Viewport = oldView;
        }

        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (!banners.ContainsKey(Logo))
            {
                GenerateData(graphicsDevice, 1, 0.5f, 0.25f);
            }

            var banner = banners[Logo];
            var oldWind = effect.WindDirection;
            var oldWindForce = effect.WindForce;
            
            var phys = Parent.GetComponent<Physics>();
            if (phys != null)
            {
                Vector3 vel = -phys.Velocity;
                if (vel.LengthSquared() > 0.001f)
                {
                    vel.Normalize();
                }
                Vector3 newWind = vel;
                float newWindForce = Math.Max(phys.Velocity.LengthSquared() * 0.005f, 0.00001f);
                effect.WindForce = prevWindForce*0.9f + newWindForce*0.1f;
                effect.WindDirection = newWind * 0.1f + prevWindDirection * 0.9f;
                prevWindForce = effect.WindForce;
                prevWindDirection = effect.WindDirection;
            }
             
            graphicsDevice.SetVertexBuffer(banner.Mesh);
            graphicsDevice.Indices = banner.Indices;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.TexturedFlag];
            Matrix world = Matrix.Identity;
            world.Translation = GlobalTransform.Translation;
            effect.World = world;
            effect.MainTexture = banner.Texture;
            effect.SelfIlluminationTexture = null;
            effect.LightRampTint = Tint;
            effect.VertexColorTint = Color.White;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, banner.Verts.Length, 0, banner.Idx.Length / 3);
            }
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
            effect.WindDirection = oldWind;
            effect.WindForce = oldWindForce;
        }
    }
}
