// Terrain2D.cs
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
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Draws the fancy scrolling background behind the main menu.
    /// </summary>
    public class Terrain2D
    {



        public struct TerrainElement
        {
            public string Name;
            public ImageFrame Image;
            public float SpawnScale;
            public float SpawnThreshold;
        }

        public TerrainElement Soil { get; set; }

        public TerrainElement Grass { get; set; }

        public TerrainElement Substrate { get; set; }

        public TerrainElement Cave { get; set; }



        public List<TerrainElement> Ores { get; set; }

        public float CaveScale { get; set; }
        public float CaveThreshold { get; set; }


        public int TileSize { get; set; }

        public float LavaHeight { get; set; }

        public Perlin Noise { get; set; }

        public float HeightScale { get; set; }

        public float SoilHeight { get; set; }

        public TerrainElement Lava { get; set; }

        public BloomComponent Bloom { get; set; }

        public float MinHeight = 0.45f;

        public Terrain2D(DwarfGame game)
        {
            Bloom = new BloomComponent(game)
            {
                Settings = BloomSettings.PresetSettings[0]
            };
            CaveScale = 0.08f;
            HeightScale = 0.01f;
            CaveThreshold = 0.5f;
            LavaHeight = 0.6f;
            TileSize = 64;
            Noise = new Perlin(1928);
            Texture2D tiles = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);

            Substrate = new TerrainElement
            {
                Image = new ImageFrame(tiles, 32, 4, 2),
                Name = "Rock"
            };

            Soil = new TerrainElement
            {
                Image = new ImageFrame(tiles, 32, 2, 0),
                Name = "Dirt"
            };


            Grass = new TerrainElement
            {
                Image = new ImageFrame(tiles, 32, 3, 0),
                Name = "Grass"
            };


            Lava = new TerrainElement
            {
                Image = new ImageFrame(tiles, 32, 0, 7),
                Name = "Lava"
            };

            Cave = new TerrainElement
            {
                Image = new ImageFrame(tiles, 32, 1, 0),
                Name = "Rock2"
            };



            Ores = new List<TerrainElement>
            {
                new TerrainElement
                {
                    Image = new ImageFrame(tiles, 32, 3, 1),
                    Name = "Gold",
                    SpawnScale = 0.05f,
                    SpawnThreshold = 0.9f
                },
                new TerrainElement
                {
                    Image = new ImageFrame(tiles, 32, 7, 1),
                    Name = "Mana",
                    SpawnScale = 0.04f,
                    SpawnThreshold = 0.9f
                }
            };

            Bloom.Initialize();

        }

        public void Render(GraphicsDevice graphics, SpriteBatch sprites, DwarfTime time)
        {
            Bloom.BeginDraw();
            sprites.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp,
                DepthStencilState.Default, RasterizerState.CullNone);
            graphics.Clear(Color.SkyBlue);
          

            Rectangle screenRect = graphics.Viewport.Bounds;

            int maxX = screenRect.Width / TileSize + 2;
            int maxY = screenRect.Width / TileSize;

            

            float t = (float)time.TotalRealTime.TotalSeconds;

            float offsetX = t * 2.0f;
            float offsetY = 0.0f;
            
            float st = (float)Math.Abs(Math.Sin(t));

            float lava = LavaHeight;
            int backSize = 2;
            
            for(int ix = 0; ix < maxX * backSize; ix++)
            {
                float x = ix + (int)(offsetX * 0.6f);

                float height = Noise.Noise(x * HeightScale * 3, 0, 100) * 0.5f + 0.6f;
                for (int iy = 0; iy < maxY * backSize; iy++)
                {
                    float y = iy + (int)offsetY;
                    float normalizedY = (1.0f) - (float)y / (float)(maxY * backSize);

                    if(normalizedY < height)
                    {
                        float tileX = ix * (TileSize / backSize) - ((offsetX * 0.6f) * (TileSize / backSize)) % (TileSize / backSize);
                        float tileY = iy * (TileSize / backSize);

                        Drawer2D.FillRect(sprites, new Rectangle((int)tileX, (int)tileY, TileSize / backSize, TileSize / backSize), new Color((int)(Color.SkyBlue.R * normalizedY * 0.8f), (int)(Color.SkyBlue.G * normalizedY * 0.8f), (int)(Color.SkyBlue.B * normalizedY)));
                    }

                }
            }
            
            
            for(int ix = 0; ix < maxX; ix++)
            {
                float x = ix + (int)offsetX;
                float height = Noise.Noise(x * HeightScale, 0, 0) * 0.8f + MinHeight;
                for (int iy = 0; iy < maxY; iy++)
                {
                    float y = iy + (int)offsetY;
                    float normalizedY = (1.0f) - (float) y / (float) maxY;

                    if(Math.Abs(normalizedY - height) < 0.01f)
                    {
                        Color tint = new Color(normalizedY, normalizedY, normalizedY);

                        RenderTile(Grass, sprites, ix, iy, offsetX, t, tint);
                    }
                    else if(normalizedY > height - 0.1f && normalizedY < height)
                    {
                        Color tint = new Color((float)Math.Pow(normalizedY, 1.5f), (float)Math.Pow(normalizedY, 1.6f), normalizedY);

                        RenderTile(Soil, sprites, ix, iy, offsetX, t, tint);
                    }
                    else if(normalizedY < height)
                    {
                        float caviness = Noise.Noise(x * CaveScale, y * CaveScale, 0);

                        if (caviness < CaveThreshold)
                        {

                            TerrainElement? oreFound = null;

                            int i = 0;
                            foreach (TerrainElement ore in Ores)
                            {
                                i++;
                                float oreNess = Noise.Noise(x * ore.SpawnScale, y * ore.SpawnScale, i);

                                if (oreNess > ore.SpawnThreshold)
                                {
                                    oreFound = ore;
                                }
                            }

                            Color tint = new Color((float)Math.Pow(normalizedY, 1.5f) * 0.5f, (float)Math.Pow(normalizedY, 1.6f) * 0.5f, normalizedY * 0.5f);

                            if (oreFound == null)
                            {
                                RenderTile(Substrate, sprites, ix, iy, offsetX, t, tint);
                            }
                            else
                            {
                                RenderTile(oreFound.Value, sprites, ix, iy, offsetX, t, tint);
                            }
                        }
                        else
                        {

                            if (normalizedY < lava)
                            {
                                float glowiness = Noise.Noise(x * CaveScale * 2, y * CaveScale * 2, t);
                                RenderTile(Lava, sprites, ix, iy, offsetX, t, new Color(0.5f * glowiness + 0.5f, 0.7f * glowiness + 0.3f * st, glowiness));
                            }
                            else
                            {
                                RenderTile(Cave, sprites, ix, iy, offsetX, t, new Color((float)Math.Pow(normalizedY, 1.5f) * (1.0f - caviness) * 0.8f, (float)Math.Pow(normalizedY, 1.6f) * (1.0f - caviness) * 0.8f, normalizedY * (1.0f - caviness)));
                            }

                        }
                    }
                    
                }
             
            }
             
             
            sprites.End();
            
            Bloom.Draw(time.ToGameTime());

        }

        public void RenderTile(TerrainElement element, SpriteBatch sprites, int ix, int iy, float x, float originX, Color tint)
        {
            float tileX = ix * TileSize - ((x) * TileSize) % TileSize;
            float tileY = iy * TileSize;

            sprites.Draw(element.Image.Image, new Rectangle((int)tileX, (int)tileY, TileSize, TileSize), element.Image.SourceRect, tint);
        }
    }
}
