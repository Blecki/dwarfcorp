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
    public class ChunkRenderer
    {
        public List<VoxelChunk> RenderList = new List<VoxelChunk>();
        private readonly Timer visibilityChunksTimer = new Timer(0.03f, false, Timer.TimerMode.Real);
        
       
        public ChunkData ChunkData;

        public ChunkRenderer(ChunkData Data)
        {
            ChunkData = Data;

            GameSettings.Default.VisibilityUpdateTime = 0.05f;
            visibilityChunksTimer = new Timer(GameSettings.Default.VisibilityUpdateTime, false, Timer.TimerMode.Real);
            visibilityChunksTimer.HasTriggered = true;
        }

        public void RenderForMinimap(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix, Texture2D tilemap)
        {
            effect.SelfIlluminationTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_illumination);
            effect.MainTexture = tilemap;
            effect.SunlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.sungradient);
            effect.AmbientOcclusionGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.ambientgradient);
            effect.TorchlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.torchgradient);
            effect.LightRamp = Color.White;
            effect.VertexColorTint = Color.White;
            effect.SelfIlluminationEnabled = true;
            effect.EnableShadows = false;
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var chunk in ChunkData.GetChunkEnumerator())
                {
                    chunk.Render(GameState.Game.GraphicsDevice);
                }
            }
            effect.SelfIlluminationEnabled = false;
        }

        public void RenderSelectionBuffer(Shader effect, GraphicsDevice graphicsDevice,
            Matrix viewmatrix)
        {
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.SelectionBuffer];
            effect.MainTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            effect.World = Matrix.Identity;
            effect.View = viewmatrix;
            effect.SelectionBufferColor = Vector4.Zero;

            if (RenderList  != null)
            foreach (VoxelChunk chunk in RenderList)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    chunk.Render(GameState.Game.GraphicsDevice);
                }
            }
        }

        public void Render(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix)
        {
            if (RenderList != null && !Debugger.Switches.HideTerrain)
            {
                foreach (VoxelChunk chunk in RenderList)
                {
                    effect.SetTexturedTechnique();
                    effect.EnableShadows = false;
                    effect.SelfIlluminationTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_illumination);
                    effect.MainTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
                    effect.SunlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.sungradient);
                    effect.AmbientOcclusionGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.ambientgradient);
                    effect.TorchlightGradient = AssetManager.GetContentTexture(ContentPaths.Gradients.torchgradient);
                    effect.LightRamp = Color.White;
                    effect.VertexColorTint = Color.White;
                    effect.SelfIlluminationEnabled = true;
                    effect.World = Matrix.Identity;
                    effect.EnableLighting = true;

                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        chunk.Render(GameState.Game.GraphicsDevice);
                    }

                    chunk.RenderMotes(GameState.Game.GraphicsDevice, effect, renderCamera);
                }                
            }

            effect.SelfIlluminationEnabled = false;
            effect.SetTexturedTechnique();
        }

        public void GetChunksIntersecting(BoundingFrustum Frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            var frustumBox = MathFunctions.GetBoundingBox(Frustum.GetCorners());
            var minChunk = ChunkData.ConfineToBounds(GlobalVoxelCoordinate.FromVector3(frustumBox.Min).GetGlobalChunkCoordinate());
            var maxChunk = ChunkData.ConfineToBounds(GlobalVoxelCoordinate.FromVector3(frustumBox.Max).GetGlobalChunkCoordinate());


            for (var x = minChunk.X; x <= maxChunk.X; ++x)
                for (var z = minChunk.Z; z <= maxChunk.Z; ++z)
                {
                    var chunkCoord = new GlobalChunkCoordinate(x, 0, z);
                    var min = new GlobalVoxelCoordinate(chunkCoord, new LocalVoxelCoordinate(0, 0, 0));
                    var box = new BoundingBox(min.ToVector3(), min.ToVector3() + new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ));
                    if (Frustum.Contains(box) != ContainmentType.Disjoint)
                        chunks.Add(ChunkData.GetChunk(chunkCoord));
                }
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
            visibilityChunksTimer.Update(gameTime);
            if (visibilityChunksTimer.HasTriggered)
            {
                var visibleSet = new HashSet<VoxelChunk>();
                GetChunksIntersecting(camera.GetDrawFrustum(), visibleSet);
                RenderList = visibleSet.ToList();
            }
            }
    }
}
