using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    /// <summary>
    /// Handles the drawing routines for liquids.
    /// </summary>
    public class WaterRenderer
    {
        private RenderTarget2D refractionRenderTarget = null;
        private RenderTarget2D reflectionRenderTarget = null;
        public Texture2D ReflectionMap = null;
        public Texture2D RefractionMap = null;

        public Dictionary<LiquidType, LiquidAsset> LiquidAssets = new Dictionary<LiquidType, LiquidAsset>();


        public bool DrawTerrainReflected
        {
            get { return GameSettings.Default.DrawChunksReflected; }
            set { GameSettings.Default.DrawChunksReflected = value; }
        }

        public bool DrawComponentsReflected
        {
            get { return GameSettings.Default.DrawEntityReflected; }
            set { GameSettings.Default.DrawEntityReflected = value; }
        }

        public bool DrawTerrainRefracted
        {
            get { return GameSettings.Default.DrawChunksRefracted; }
            set { GameSettings.Default.DrawChunksRefracted = value; }
        }

        public bool DrawComponentsRefracted
        {
            get { return GameSettings.Default.DrawEntityRefracted; }
            set { GameSettings.Default.DrawEntityRefracted = value; }
        }


        public void AddLiquidAsset(LiquidAsset asset)
        {
            LiquidAssets[asset.Type] = asset;
        }

        public WaterRenderer(GraphicsDevice device)
        {
            int width = device.Viewport.Width / 4;
            int height = device.Viewport.Height / 4;

            while(width <= 0 || height <= 0)
            {
                width = device.Viewport.Width / 4;
                height = device.Viewport.Height / 4;
                Thread.Sleep(100);
            }

            ReflectionMap = new Texture2D(device, width, height);
            RefractionMap = new Texture2D(device, width, height);
            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);
            reflectionRenderTarget = new RenderTarget2D(device, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);
        }

        public Plane CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if(clipSide)
            {
                planeCoeffs *= -1;
            }
            Plane finalPlane = new Plane(planeCoeffs);
            return finalPlane;
        }

        public float GetVisibleWaterHeight(ChunkManager chunkManager, Camera camera, Viewport port, float defaultHeight)
        {
            Voxel vox = chunkManager.ChunkData.GetFirstVisibleBlockHitByScreenCoord(port.Width / 2, port.Height / 2, camera, port, 100.0f);

            if(vox != null)
            {
                float h = vox.Chunk.GetTotalWaterHeightCells(vox) - 0.75f;
                if(h < 0.01f)
                {
                    return defaultHeight;
                }

                return (h + vox.Position.Y + defaultHeight) / 2.0f + 0.5f;
            }
            else
            {
                return defaultHeight;
            }
        }

        public void DrawRefractionMap(GameTime gameTime, PlayState game, float waterHeight, Matrix viewMatrix, Effect effect, GraphicsDevice device)
        {
            Plane refractionPlane = CreatePlane(waterHeight, new Vector3(0, -1, 0), viewMatrix, false);

            effect.Parameters["ClipPlane0"].SetValue(new Vector4(refractionPlane.Normal, refractionPlane.D));
            effect.Parameters["Clipping"].SetValue(1);
            effect.Parameters["GhostMode"].SetValue(0);
            device.SetRenderTarget(refractionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);


            if(DrawTerrainRefracted)
            {
                game.Draw3DThings(gameTime, effect, viewMatrix);
            }
            else
            {
                game.DrawSky(gameTime, viewMatrix, 0.25f);
            }

            Drawer3D.Render(device, effect, false);

            if(DrawComponentsRefracted)
            {
                game.DrawComponents(gameTime, effect, viewMatrix, ComponentManager.WaterRenderType.Refractive, waterHeight);
            }
            else
            {
                game.DrawSky(gameTime, viewMatrix, 0.25f);
            }


            device.SetRenderTarget(null);
            effect.Parameters["Clipping"].SetValue(0);
            RefractionMap = refractionRenderTarget;
        }


        public void DrawReflectionMap(GameTime gameTime, PlayState game, float waterHeight, Matrix reflectionViewMatrix, Effect effect, GraphicsDevice device)
        {
            Plane reflectionPlane = CreatePlane(waterHeight, new Vector3(0, -1, 0), reflectionViewMatrix, true);

            effect.Parameters["ClipPlane0"].SetValue(new Vector4(reflectionPlane.Normal, reflectionPlane.D));
            effect.Parameters["Clipping"].SetValue(1);
            effect.Parameters["GhostMode"].SetValue(0);
            device.SetRenderTarget(reflectionRenderTarget);


            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);
            effect.Parameters["xView"].SetValue(reflectionViewMatrix);

            //game.DrawSky();

            if(DrawTerrainReflected)
            {
                game.DrawSky(gameTime, reflectionViewMatrix, 0.25f);
                game.Draw3DThings(gameTime, effect, reflectionViewMatrix);
            }
            else
            {
                game.DrawSky(gameTime, reflectionViewMatrix, 0.25f);
            }

            Drawer3D.Render(device, effect, false);

            if(DrawComponentsReflected)
            {
                effect.Parameters["xView"].SetValue(reflectionViewMatrix);
                game.DrawComponents(gameTime, effect, reflectionViewMatrix, ComponentManager.WaterRenderType.Reflective, waterHeight);
            }

            effect.Parameters["Clipping"].SetValue(0);
            device.SetRenderTarget(null);

            ReflectionMap = reflectionRenderTarget;
        }

        public void DrawWaterFlat(GraphicsDevice device, Matrix view, Matrix projection, Effect effect, ChunkManager chunks)
        {
            effect.CurrentTechnique = effect.Techniques["WaterFlat"];
            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(view);
            effect.Parameters["xProjection"].SetValue(projection);

            foreach(KeyValuePair<LiquidType, LiquidAsset> asset in LiquidAssets)
            {
                effect.Parameters["xFlatColor"].SetValue(asset.Value.FlatColor);

                foreach(KeyValuePair<Point3, VoxelChunk> chunkpair in chunks.ChunkData.ChunkMap)
                {
                    VoxelChunk chunk = chunkpair.Value;

                    foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        if(chunk.IsVisible)
                        {
                            //chunk.PrimitiveMutex.WaitOne();
                            chunk.Liquids[asset.Key].Render(device);
                            //chunk.PrimitiveMutex.ReleaseMutex();
                        }
                    }
                }
            }
        }
        

        public void DrawWater(GraphicsDevice device,
            float time,
            Effect effect,
            Matrix viewMatrix,
            Matrix reflectionViewMatrix,
            Matrix projectionMatrix,
            Vector3 windDirection,
            Camera camera,
            ChunkManager chunks)
        {
            effect.CurrentTechnique = effect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xReflectionView"].SetValue(reflectionViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xReflectionMap"].SetValue(ReflectionMap);
            effect.Parameters["xRefractionMap"].SetValue(RefractionMap);
            effect.Parameters["xTime"].SetValue(time);
            effect.Parameters["xWindDirection"].SetValue(windDirection);
            effect.Parameters["xCamPos"].SetValue(camera.Position);

            foreach(KeyValuePair<LiquidType, LiquidAsset> asset in LiquidAssets)
            {
                effect.Parameters["xWaveLength"].SetValue(asset.Value.WaveLength);
                effect.Parameters["xWaveHeight"].SetValue(asset.Value.WaveHeight);
                effect.Parameters["xWindForce"].SetValue(asset.Value.WindForce);
                effect.Parameters["xWaterBumpMap"].SetValue(asset.Value.BumpTexture);
                effect.Parameters["xTexture"].SetValue(asset.Value.BaseTexture);
                effect.Parameters["xTexture1"].SetValue(asset.Value.FoamTexture);
                effect.Parameters["xWaterOpacity"].SetValue(asset.Value.Opactiy);
                effect.Parameters["xWaterMinOpacity"].SetValue(asset.Value.MinOpacity);
                effect.Parameters["xWaterSloshOpacity"].SetValue(asset.Value.SloshOpacity);
                effect.Parameters["xRippleColor"].SetValue(asset.Value.RippleColor);

                foreach(KeyValuePair<Point3, VoxelChunk> chunkpair in chunks.ChunkData.ChunkMap)
                {
                    VoxelChunk chunk = chunkpair.Value;

                    foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        if(chunk.IsVisible)
                        {
                            //chunk.PrimitiveMutex.WaitOne();
                            chunk.Liquids[asset.Key].Render(device);
                            //chunk.PrimitiveMutex.ReleaseMutex();
                        }
                    }
                }
            }
        }
    }

}