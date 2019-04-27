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
        public List<VoxelChunk> LiveVoxelList = new List<VoxelChunk>();
        private int RenderCycle = 1;


        public ChunkManager ChunkData;

        public ChunkRenderer(ChunkManager Data)
        {
            ChunkData = Data;

            GameSettings.Default.VisibilityUpdateTime = 0.05f;
        }

        public void RenderForMinimap(Camera renderCamera, DwarfTime gameTime, GraphicsDevice graphicsDevice, Shader effect, Matrix worldMatrix, Texture2D tilemap)
        {
            // Todo: Render to a texture stored in the chunk; render that texture to the screen.

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

        private void GetChunksIntersecting(BoundingFrustum Frustum, HashSet<VoxelChunk> chunks)
        {
            chunks.Clear();
            var frustumBox = MathFunctions.GetBoundingBox(Frustum.GetCorners());
            var minChunk = ChunkData.ConfineToBounds(GlobalVoxelCoordinate.FromVector3(frustumBox.Min).GetGlobalChunkCoordinate());
            var maxChunk = ChunkData.ConfineToBounds(GlobalVoxelCoordinate.FromVector3(frustumBox.Max).GetGlobalChunkCoordinate());


            for (var x = minChunk.X; x <= maxChunk.X; ++x)
                for (var y = minChunk.Y; y <= maxChunk.Y; ++y)
                    for (var z = minChunk.Z; z <= maxChunk.Z; ++z)
                    {
                        var chunkCoord = new GlobalChunkCoordinate(x, y, z);
                        var min = new GlobalVoxelCoordinate(chunkCoord, new LocalVoxelCoordinate(0, 0, 0));
                        var box = new BoundingBox(min.ToVector3(), min.ToVector3() + new Vector3(VoxelConstants.ChunkSizeX, VoxelConstants.ChunkSizeY, VoxelConstants.ChunkSizeZ));
                        if (Frustum.Contains(box) != ContainmentType.Disjoint)
                            chunks.Add(ChunkData.GetChunk(chunkCoord));
                    }
        }

        public void Update(DwarfTime gameTime, Camera camera, GraphicsDevice g)
        {
                
                var visibleSet = new HashSet<VoxelChunk>();
                GetChunksIntersecting(camera.GetDrawFrustum(), visibleSet);
                RenderList = visibleSet.ToList();
                foreach (var chunk in visibleSet)
                {
                    if (chunk.Visible == false)
                    {
                        chunk.Visible = true;
                        chunk.Manager.InvalidateChunk(chunk);
                    }

                    chunk.RenderCycleWhenLastVisible = RenderCycle;
                }

                foreach (var chunk in RenderList)
                {
                    if (chunk.RenderCycleWhenLastVisible != RenderCycle)
                        chunk.Visible = false;
                }

                RenderList = visibleSet.ToList();
                RenderCycle += 1;
        }
    }
}
