using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    /// <summary>
    /// Handles the drawing routines for liquids.
    /// </summary>
    public class WaterRenderer : IDisposable
    {
        private RenderTarget2D reflectionRenderTarget = null;
        public Texture2D ReflectionMap = null;
        public Texture2D ShoreMap = null;

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

        public bool DrawSkyReflected
        {
            get { return GameSettings.Default.DrawSkyReflected; }
            set { GameSettings.Default.DrawSkyReflected = value; }
        }

        public bool DrawReflections
        {
            get { return DrawSkyReflected || DrawTerrainReflected || DrawComponentsReflected; }
        }


        public void AddLiquidAsset(LiquidAsset asset)
        {
            LiquidAssets[asset.Type] = asset;
        }

        public WaterRenderer(GraphicsDevice device)
        {

        }
        
        public void CreateContent(GraphicsDevice device)
        {
            LiquidAssets.Clear();
            PresentationParameters pp = device.PresentationParameters;

            int width = Math.Min(pp.BackBufferWidth / 4, 4096);
            int height = Math.Min(pp.BackBufferHeight / 4, 4096);
            ReflectionMap = new Texture2D(device, width, height);
            reflectionRenderTarget = new RenderTarget2D(device, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);
            ShoreMap = AssetManager.GetContentTexture(ContentPaths.Gradients.shoregradient);

            LiquidAsset waterAsset = new LiquidAsset
            {
                Type = LiquidType.Water,
                Opactiy = 0.8f,
                Reflection = 1.0f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                BumpTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.water_normal),
                BaseTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.cartoon_water),
                MinOpacity = 0.4f,
                RippleColor = new Vector4(0.6f, 0.6f, 0.6f, 0.0f),
                FlatColor = new Vector4(0.3f, 0.3f, 0.9f, 1.0f)
            };
            AddLiquidAsset(waterAsset);


            LiquidAsset lavaAsset = new LiquidAsset
            {
                Type = LiquidType.Lava,
                Opactiy = 0.95f,
                Reflection = 0.0f,
                WaveHeight = 0.1f,
                WaveLength = 0.05f,
                WindForce = 0.001f,
                MinOpacity = 0.8f,
                BumpTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.water_normal),
                BaseTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.lava),
                RippleColor = new Vector4(0.5f, 0.4f, 0.04f, 0.0f),
                FlatColor = new Vector4(0.9f, 0.7f, 0.2f, 1.0f)
            };

            AddLiquidAsset(lavaAsset);
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

        public static float GetTotalWaterHeightCells(ChunkManager ChunkManager, VoxelHandle vox)
        {
            float tot = 0;

            var localVoxelCoordinate = vox.Coordinate.GetLocalVoxelCoordinate();
            for (var y = vox.Coordinate.Y; y < ChunkManager.World.WorldSizeInVoxels.Y; y++)
            {
                var v = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(vox.Coordinate.X, y, vox.Coordinate.Z));
                tot += v.LiquidLevel / (float)WaterManager.maxWaterLevel;
                if (y > vox.Coordinate.Y && v.LiquidLevel == 0)
                    return tot;
            }

            return tot;
        }

        public float GetVisibleWaterHeight(ChunkManager chunkManager, Camera camera, Viewport port, float defaultHeight)
        {
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(chunkManager, port.Width / 2, port.Height / 2, camera, port, 100.0f, false, null);

            if(vox.IsValid)
            {
                float h = GetTotalWaterHeightCells(chunkManager, vox) - 0.75f;
                if(h < 0.01f)
                    return defaultHeight;

                return (h + vox.Coordinate.Y + defaultHeight) / 2.0f + 0.5f;
            }
            else
            {
                return defaultHeight;
            }
        }
        private Timer reflectionTimer = new Timer(0.1f, false, Timer.TimerMode.Real);
        private Vector3 prevCameraPos = Vector3.Zero;
        private Vector3 prevCameraTarget = Vector3.Zero;

        public void DrawReflectionMap(IEnumerable<GameComponent> Renderables, DwarfTime gameTime, WorldManager game, float waterHeight, Matrix reflectionViewMatrix, Shader effect, GraphicsDevice device)
        {
            if (!DrawReflections) return;
            ValidateBuffers();
            reflectionTimer.Update(gameTime);
            if (!reflectionTimer.HasTriggered && (prevCameraPos - game.Renderer.Camera.Position).LengthSquared() < 0.001 && (prevCameraTarget - game.Renderer.Camera.Target).LengthSquared() < 0.001)
                return;

            prevCameraPos = game.Renderer.Camera.Position;
            prevCameraTarget = game.Renderer.Camera.Target;

            Plane reflectionPlane = CreatePlane(waterHeight, new Vector3(0, -1, 0), reflectionViewMatrix, true);

            effect.ClipPlane = new Vector4(reflectionPlane.Normal, reflectionPlane.D);
            effect.ClippingEnabled = true;
            effect.GhostClippingEnabled = false;
            device.SetRenderTarget(reflectionRenderTarget);


            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);
            effect.View = reflectionViewMatrix;

            //game.DrawSky();

            if(DrawTerrainReflected)
            {
                game.Renderer.DrawSky(gameTime, reflectionViewMatrix, 0.25f, effect.FogColor, false);
                game.Renderer.Draw3DThings(gameTime, effect, reflectionViewMatrix);
            }
            else
            {
                game.Renderer.DrawSky(gameTime, reflectionViewMatrix, 0.25f, effect.FogColor, false);
            }

            effect.View = reflectionViewMatrix;
            //Drawer3D.Render(device, effect, false);

            if(DrawComponentsReflected)
            {
                effect.View = reflectionViewMatrix;
                ComponentRenderer.Render(Renderables, gameTime, game.ChunkManager, game.Renderer.Camera,
                    DwarfGame.SpriteBatch, game.GraphicsDevice, effect,
                    ComponentRenderer.WaterRenderType.Reflective, waterHeight);
                game.Renderer.InstanceRenderer.Flush(device, effect, game.Renderer.Camera, InstanceRenderMode.Normal);
            }

            effect.ClippingEnabled = false;
            device.SetRenderTarget(null);

            ReflectionMap = reflectionRenderTarget;
        }

        public void ValidateBuffers()
        {
            if (reflectionRenderTarget == null || reflectionRenderTarget.IsContentLost || reflectionRenderTarget.IsContentLost ||
                ShoreMap == null || ShoreMap.IsDisposed || ShoreMap.GraphicsDevice.IsDisposed)
            {
                CreateContent(GameState.Game.GraphicsDevice);
            }
        }

        public void DrawWater(GraphicsDevice device,
            float time,
            Shader effect,
            Matrix viewMatrix,
            Matrix reflectionViewMatrix,
            Matrix projectionMatrix,
            Vector3 windDirection,
            Camera camera,
            ChunkManager chunks)
        {
            try
            {
                ValidateBuffers();
                if (DrawReflections)
                {
                    effect.CurrentTechnique = effect.Techniques[Shader.Technique.Water];
                }
                else
                {
                    effect.CurrentTechnique = effect.Techniques[Shader.Technique.WaterTextured];
                }

                BlendState origState = device.BlendState;
                DepthStencilState origDepthState = device.DepthStencilState;
                device.DepthStencilState = DepthStencilState.Default;

                device.BlendState = BlendState.NonPremultiplied;


                Matrix worldMatrix = Matrix.Identity;
                effect.World = worldMatrix;
                effect.View = viewMatrix;
                effect.CameraPosition = camera.Position;
                if (DrawReflections)
                {
                    effect.ReflectionView = reflectionViewMatrix;
                }

                effect.Projection = projectionMatrix;

                if (DrawReflections)
                    effect.WaterReflectionMap = ReflectionMap;

                effect.WaterShoreGradient = ShoreMap;
                effect.Time = time;
                effect.WindDirection = windDirection;
                effect.CameraPosition = camera.Position;


                foreach (KeyValuePair<LiquidType, LiquidAsset> asset in LiquidAssets)
                {

                    effect.WaveLength = asset.Value.WaveLength;
                    effect.WaveHeight = asset.Value.WaveHeight;
                    if (DrawReflections)
                    {
                        effect.WaterBumpMap = asset.Value.BumpTexture;
                        effect.WaterReflectance = asset.Value.Reflection;
                    }
                    effect.MainTexture = asset.Value.BaseTexture;
                    effect.WaterOpacity = asset.Value.Opactiy;
                    effect.MinWaterOpacity = asset.Value.MinOpacity;
                    effect.RippleColor = new Color(asset.Value.RippleColor);


                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        foreach (var chunk in chunks.World.Renderer.ChunkRenderer.RenderList)
                            chunk.Liquids[asset.Key].Render(device);
                    }
                }
                device.BlendState = origState;
                device.DepthStencilState = origDepthState;
            }
            catch (Exception exception)
            {
                Console.Out.WriteLine(exception);
                return;
            }
        }

        public void Dispose()
        {
            reflectionRenderTarget.Dispose();
            ReflectionMap.Dispose();
        }
    }

}
