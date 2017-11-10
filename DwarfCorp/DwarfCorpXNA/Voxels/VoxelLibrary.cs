// VoxelLibrary.cs
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
//using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A static collection of voxel types and their properties.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelLibrary
    {
        /// <summary>
        /// Specifies that a specific voxel is a resource which should
        /// spawn in veins.
        /// </summary>
        public class ResourceSpawnRate
        {
            public float VeinSize;
            public float VeinSpawnThreshold;
            public float MinimumHeight;
            public float MaximumHeight;
            public float Probability;
        }


        public static Dictionary<VoxelType, BoxPrimitive> PrimitiveMap = new Dictionary<VoxelType, BoxPrimitive>();
        public static VoxelType emptyType = null;

        public static Dictionary<string, VoxelType> Types = new Dictionary<string, VoxelType>();
        public static List<VoxelType> TypeList;

        public VoxelLibrary()
        {
        }

        private static VoxelType.FringeTileUV[] CreateFringeUVs(Point[] Tiles)
        {
            System.Diagnostics.Debug.Assert(Tiles.Length == 3);

            var r = new VoxelType.FringeTileUV[8];

            // North
            r[0] = new VoxelType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2) + 1, 16, 32);
            // East
            r[1] = new VoxelType.FringeTileUV((Tiles[1].X * 2) + 1, Tiles[1].Y, 32, 16);
            // South
            r[2] = new VoxelType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2), 16, 32);
            // West
            r[3] = new VoxelType.FringeTileUV(Tiles[1].X * 2, Tiles[1].Y, 32, 16);

            // NW
            r[4] = new VoxelType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2) + 1, 32, 32);
            // NE
            r[5] = new VoxelType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2) + 1, 32, 32);
            // SE
            r[6] = new VoxelType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2), 32, 32);
            // SW
            r[7] = new VoxelType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2), 32, 32);

            return r;
        }

        public static Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords> CreateTransitionUVs(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point[] tiles,  VoxelType.TransitionType transitionType = VoxelType.TransitionType.Horizontal)
        {
            var transitionTextures = new Dictionary<BoxTransition, BoxPrimitive.BoxTextureCoords>();

            for(int i = 0; i < 16; i++)
            {
                Point topPoint = new Point(tiles[0].X + i, tiles[0].Y);

                if (transitionType == VoxelType.TransitionType.Horizontal)
                {
                    BoxTransition transition = new BoxTransition()
                    {
                        Top = (TransitionTexture) i
                    };
                    transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                        textureMap.Height, width, height, tiles[2], tiles[2], topPoint, tiles[1], tiles[2], tiles[2]);
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                    { 
                         Point sidePoint = new Point(tiles[0].X + j, tiles[0].Y);
                        // TODO: create every iteration of frontback vs. left right. There should be 16 of these.
                        BoxTransition transition = new BoxTransition()
                        {
                            Left = (TransitionTexture)i,
                            Right = (TransitionTexture)i,
                            Front = (TransitionTexture)j,
                            Back = (TransitionTexture)j
                        };
                        transitionTextures[transition] = new BoxPrimitive.BoxTextureCoords(textureMap.Width,
                            textureMap.Height, width, height, sidePoint, sidePoint, tiles[2], tiles[1], topPoint, topPoint);
                    }
                }
            }

            return transitionTextures;
        }

        public static BoxPrimitive CreatePrimitive(GraphicsDevice graphics, Texture2D textureMap, int width, int height, Point top, Point sides, Point bottom)
        {
            BoxPrimitive.BoxTextureCoords coords = new BoxPrimitive.BoxTextureCoords(textureMap.Width, textureMap.Height, width, height, sides, sides, top, bottom, sides, sides);
            BoxPrimitive cube = new BoxPrimitive(graphics, 1.0f, 1.0f, 1.0f, coords);

            return cube;
        }

        public static void InitializeDefaultLibrary(GraphicsDevice graphics, Texture2D cubeTexture)
        {
            TypeList = FileUtils.LoadJson<List<VoxelType>>(ContentPaths.voxel_types, false);
            emptyType = TypeList[0];

            short ID = 0;
            foreach (VoxelType type in TypeList)
            {
                type.ID = ID;
                ++ID;

                Types[type.Name] = type;
                PrimitiveMap[type] = type.ID == 0 ? null : CreatePrimitive(graphics, cubeTexture, 32, 32, type.Top, type.Bottom, type.Sides);

                if (type.HasTransitionTextures)
                    type.TransitionTextures = CreateTransitionUVs(graphics, cubeTexture, 32, 32, type.TransitionTiles, type.Transitions);

                if (type.HasFringeTransitions)
                    type.FringeTransitionUVs = CreateFringeUVs(type.FringeTiles);

                type.ExplosionSound = SoundSource.Create(type.ExplosionSoundResource);
                type.HitSound = SoundSource.Create(type.HitSoundResources);
            }
        }

        public static VoxelType GetVoxelType(short id)
        {
            return TypeList[id];
        }

        public static VoxelType GetVoxelType(string name)
        {
            return Types[name];
        }

        public static BoxPrimitive GetPrimitive(string name)
        {
            return (from v in PrimitiveMap.Keys
                where v.Name == name
                select GetPrimitive(v)).FirstOrDefault();
        }

        public static BoxPrimitive GetPrimitive(VoxelType type)
        {
            if(PrimitiveMap.ContainsKey(type))
            {
                return PrimitiveMap[type];
            }
            else
            {
                return null;
            }
        }

        public static BoxPrimitive GetPrimitive(short id)
        {
            return GetPrimitive(GetVoxelType(id));
        }

        public static List<VoxelType> GetTypes()
        {
            return PrimitiveMap.Keys.ToList();
        }

        // Do not delete: Used to generate block icon texture for menu.
        public static Texture2D RenderIcons(GraphicsDevice device, Shader shader, ChunkManager chunks, int width, int height, int tileSize)
        {
            RenderTarget2D toReturn = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.Depth16, 16, RenderTargetUsage.PreserveContents);
        
            device.SetRenderTarget(toReturn);
            device.Clear(Color.Transparent);
            shader.SetTexturedTechnique();
            shader.MainTexture = chunks.ChunkData.Tilemap;
            shader.SelfIlluminationEnabled = true;
            shader.SelfIlluminationTexture = chunks.ChunkData.IllumMap;
            shader.EnableShadows = false;
            shader.EnableLighting = false;
            shader.ClippingEnabled = false;
            shader.CameraPosition = new Vector3(-0.5f, 0.5f, 0.5f);
            shader.VertexColorTint = Color.White;
            shader.LightRampTint = Color.White;
            shader.SunlightGradient = chunks.ChunkData.SunMap;
            shader.AmbientOcclusionGradient = chunks.ChunkData.AmbientMap;
            shader.TorchlightGradient = chunks.ChunkData.TorchMap;
            Viewport oldview = device.Viewport;
            List<VoxelType> voxelsByType = Types.Select(type => type.Value).ToList();
            voxelsByType.Sort((a, b) => a.ID < b.ID ? -1 : 1);
            int rows = width/tileSize;
            int cols = height/tileSize;
            device.ScissorRectangle = new Rectangle(0, 0, tileSize, tileSize);
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.Default;
            Vector3 half = Vector3.One*0.5f;
            half = new Vector3(half.X, half.Y + 0.3f, half.Z);
            foreach (EffectPass pass in shader.CurrentTechnique.Passes)
            {
                foreach (var type in voxelsByType)
                {
                    int row = type.ID/cols;
                    int col = type.ID%cols;
                    BoxPrimitive primitive = GetPrimitive(type);
                    if (primitive == null)
                        continue;

                    if (type.HasTransitionTextures)
                        primitive = new BoxPrimitive(device, 1, 1, 1, type.TransitionTextures[new BoxTransition()]);

                    device.Viewport = new Viewport(col * tileSize, row * tileSize, tileSize, tileSize);
                    Matrix viewMatrix = Matrix.CreateLookAt(new Vector3(-1.5f, 1.3f, -1.5f), Vector3.Zero, Vector3.Up);
                    Matrix projectionMatrix = Matrix.CreateOrthographic(1.75f, 1.75f, 0, 5);
                    shader.View = viewMatrix;
                    shader.Projection = projectionMatrix;
                    shader.World = Matrix.CreateTranslation(-half);
                    pass.Apply();
                    primitive.Render(device);
                }
            }
            device.Viewport = oldview;
            return (Texture2D) toReturn;
        }
    }

}