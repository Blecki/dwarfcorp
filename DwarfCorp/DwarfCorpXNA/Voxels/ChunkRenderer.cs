// ChunkManager.cs
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
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{

    /// <summary>
    /// Responsible for keeping track of and accessing large collections of
    /// voxels. There is intended to be only one chunk manager. Essentially,
    /// it is a virtual memory lookup table for the world's voxels. It imitates
    /// a gigantic 3D array.
    /// </summary>
    public class ChunkRenderer
    {
        public ConcurrentQueue<VoxelChunk> RenderList { get; set; }

        private readonly Timer visibilityChunksTimer = new Timer(0.03f, false, Timer.TimerMode.Real);
        
        public float DrawDistance
        {
            get { return GameSettings.Default.ChunkDrawDistance; }
        }
        
        public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }

        public Camera camera = null;
        public WorldManager World { get; set; }

        private readonly HashSet<VoxelChunk> visibleSet = new HashSet<VoxelChunk>();

        public ChunkData ChunkData;

        public ChunkRenderer( 
            WorldManager world,
            Camera camera, 
            GraphicsDevice graphics,
            ChunkData Data)
        {
            World = world;
            ChunkData = Data;
            RenderList = new ConcurrentQueue<VoxelChunk>();

            ChunkData.MaxViewingLevel = VoxelConstants.ChunkSizeY;

            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            visibilityChunksTimer = new Timer(GameSettings.Default.VisibilityUpdateTime, false, Timer.TimerMode.Real);
            visibilityChunksTimer.HasTriggered = true;
            this.camera = camera;
        }

        public void UpdateRenderList(Camera camera)
        {
            while(RenderList.Count > 0)
            {
                VoxelChunk result;
                if(!RenderList.TryDequeue(out result))
                {
                    break;
                }
            }


            foreach(VoxelChunk chunk in visibleSet)
            {
                BoundingBox box = chunk.GetBoundingBox();

                if((camera.Position - (box.Min + box.Max) * 0.5f).Length() < DrawDistance)
                {
                    chunk.IsVisible = true;
                    RenderList.Enqueue(chunk);
                }
                else
                {
                    chunk.IsVisible = false;
                }
            }
        }

        public void RenderAll(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix, Texture2D tilemap)
        {
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = false;

			BoundingFrustum cameraFrustrum = renderCamera.GetFrustrum();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.GetChunkEnumerator())
                {
                    if (cameraFrustrum.Intersects(chunk.GetBoundingBox()))
                    {
                        chunk.Render(Graphics);
                    }
                }
            }
            effect.SelfIlluminationEnabled = false;
        }

        public void RenderSelectionBuffer(Shader effect, GraphicsDevice graphicsDevice,
            Matrix viewmatrix)
        {
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.SelectionBuffer];
            effect.MainTexture = ChunkData.Tilemap;
            effect.World = Matrix.Identity;
            effect.View = viewmatrix;
            effect.SelectionBufferColor = Vector4.Zero;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
        }

        public void RenderShadowmap(Shader effect,
                                    GraphicsDevice graphicsDevice, 
                                    ShadowRenderer shadowRenderer,
                                    Matrix worldMatrix, 
                                    Texture2D tilemap)
        {
            Vector3[] corners = new Vector3[8];
            if (camera == null)
            {
                camera = World.Camera;
            }
            Camera tempCamera = new Camera(World, camera.Target, camera.Position, camera.FOV, camera.AspectRatio, camera.NearPlane, 30);
            tempCamera.GetFrustrum().GetCorners(corners);
            BoundingBox cameraBox = MathFunctions.GetBoundingBox(corners);
            cameraBox = cameraBox.Expand(1.0f);
            effect.World = worldMatrix;
            effect.MainTexture = tilemap;
            shadowRenderer.SetupViewProj(cameraBox);
            shadowRenderer.PrepareEffect(effect, false);
            shadowRenderer.BindShadowmapEffect(effect);
            shadowRenderer.BindShadowmap(graphicsDevice);

            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
            shadowRenderer.UnbindShadowmap(graphicsDevice);
            effect.SetTexturedTechnique();
            effect.SelfIlluminationEnabled = false;
        }

        public void RenderLightmaps(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice,
            Shader effect, Matrix worldMatrix)
        {
            RasterizerState state = RasterizerState.CullNone;
            RasterizerState origState = graphicsDevice.RasterizerState;
            BlendState origBlendState = graphicsDevice.BlendState;
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Lightmap];
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = ChunkData.Tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            effect.EnableLighting = true;
            graphicsDevice.RasterizerState = state;
            Graphics.BlendState = BlendState.NonPremultiplied;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (VoxelChunk chunk in renderListCopy)
                {
                    Graphics.SetRenderTarget(chunk.Primitive.Lightmap);
                    Graphics.Clear(ClearOptions.Target, Color.Transparent, 1.0f, 0);
                    chunk.Render(Graphics);
                }
            }
            Graphics.SetRenderTarget(null);
            effect.SelfIlluminationEnabled = false;
            effect.SetTexturedTechnique();
            graphicsDevice.RasterizerState = origState;
            graphicsDevice.BlendState = origBlendState;
        }

        public void Render(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix)
        {
            if (GameSettings.Default.UseLightmaps)
            {
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.TexturedWithLightmap];
                effect.EnableShadows = false;
            }
            else
            {
                effect.SetTexturedTechnique();
                effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            }
            effect.SelfIlluminationTexture = ChunkData.IllumMap;
            effect.MainTexture = ChunkData.Tilemap;
            effect.SunlightGradient = ChunkData.SunMap;
            effect.AmbientOcclusionGradient = ChunkData.AmbientMap;
            effect.TorchlightGradient = ChunkData.TorchMap;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = GameSettings.Default.UseDynamicShadows;
            effect.World = Matrix.Identity;
            effect.EnableLighting = true;
            List<VoxelChunk> renderListCopy = RenderList.ToArray().ToList();

            foreach (VoxelChunk chunk in renderListCopy)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    if (GameSettings.Default.UseLightmaps && chunk.Primitive.Lightmap != null)
                    {
                        effect.LightMap = chunk.Primitive.Lightmap;
                        effect.PixelSize = new Vector2(1.0f/chunk.Primitive.Lightmap.Width,
                            1.0f/chunk.Primitive.Lightmap.Height);
                    }
                    pass.Apply();
                    chunk.Render(Graphics);
                }
            }
            effect.SelfIlluminationEnabled = false;
            effect.SetTexturedTechnique();
        }

        public void GetChunksIntersecting(BoundingBox box, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            var minChunk = GlobalVoxelCoordinate.FromVector3(box.Min).GetGlobalChunkCoordinate();
            var maxChunk = GlobalVoxelCoordinate.FromVector3(box.Max).GetGlobalChunkCoordinate();
            for (var x = minChunk.X; x <= maxChunk.X; ++x)
                for (var y = minChunk.Y; y <= maxChunk.Y; ++y)
                    for (var z = minChunk.Z; z <= maxChunk.Z; ++z)
                    {
                        var coord = new GlobalChunkCoordinate(x, y, z);
                        if (ChunkData.CheckBounds(coord))
                            chunks.Add(ChunkData.GetChunk(coord));
                    }
        }

        public void GetChunksIntersecting(BoundingFrustum frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            BoundingBox frustumBox = MathFunctions.GetBoundingBox(frustum.GetCorners());
            GetChunksIntersecting(frustumBox, chunks);

            chunks.RemoveWhere(chunk => frustum.Contains(chunk.GetBoundingBox()) == ContainmentType.Disjoint);
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            UpdateRenderList(camera);

            visibilityChunksTimer.Update(gameTime);
            if(visibilityChunksTimer.HasTriggered)
            {
                visibleSet.Clear();
                GetChunksIntersecting(camera.GetFrustrum(), visibleSet);
            }
        }
    }
}
