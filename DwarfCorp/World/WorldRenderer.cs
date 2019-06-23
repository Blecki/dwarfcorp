using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Events;

namespace DwarfCorp
{
    public partial class WorldRenderer : IDisposable
    {
        public WorldManager World;
        public float CaveView = 0;
        public float TargetCaveView = 0;
        public Overworld Settings = null;
        public WorldRendererPersistentSettings PersistentSettings = new WorldRendererPersistentSettings();
        public Vector3 CursorLightPos = Vector3.Zero;
        public Vector3[] LightPositions = new Vector3[16];
        public Shader DefaultShader;
        public OrbitCamera Camera;
        public static int MultiSamples
        {
            get { return GameSettings.Default.AntiAliasing; }
            set { GameSettings.Default.AntiAliasing = value; }
        }

        public bool UseFXAA
        {
            get { return MultiSamples == -1; }
        }

        public BloomComponent bloom;
        public FXAA fxaa;
        public ChunkRenderer ChunkRenderer;
        public WaterRenderer WaterRenderer;
        public SkyRenderer Sky;
        public SelectionBuffer SelectionBuffer;
        public InstanceRenderer InstanceRenderer;
        public ContentManager Content;
        public DwarfGame Game;
        public GraphicsDevice GraphicsDevice { get { return GameState.Game.GraphicsDevice; } }

        // Hack to smooth water reflections TODO: Put into water manager
        private float lastWaterHeight = -1.0f;

        public DesignationDrawer DesignationDrawer = new DesignationDrawer();

        public struct Screenshot
        {
            public string FileName { get; set; }
            public Point Resolution { get; set; }
        }

        public List<Screenshot> Screenshots { get; set; }
        public object ScreenshotLock = new object();

        /// <summary>
        /// Creates a new play state
        /// </summary>
        /// <param name="Game">The program currently running</param>
        public WorldRenderer(DwarfGame Game, WorldManager World)
        {
            this.World = World;
            this.Game = Game;
            Content = Game.Content;
            PersistentSettings.MaxViewingLevel = World.WorldSizeInVoxels.Y;
        }

        /// <summary>
        /// Creates a screenshot of the game and saves it to a file.
        /// </summary>
        /// <param name="filename">The file to save the screenshot to</param>
        /// <param name="resolution">The width/height of the image</param>
        /// <returns>True if the screenshot could be taken, false otherwise</returns>
        public bool TakeScreenshot(string filename, Point resolution)
        {
            try
            {
                using (
                    RenderTarget2D renderTarget = new RenderTarget2D(GraphicsDevice, resolution.X, resolution.Y, false,
                        SurfaceFormat.Color, DepthFormat.Depth24))
                {
                    var frustum = Camera.GetDrawFrustum();
                    var renderables = World.EnumerateIntersectingObjects(frustum)
                        .Where(r => r.IsVisible && !World.ChunkManager.IsAboveCullPlane(r.GetBoundingBox()));

                    var oldProjection = Camera.ProjectionMatrix;
                    Matrix projectionMatrix = Matrix.CreatePerspectiveFieldOfView(Camera.FOV, ((float)resolution.X) / resolution.Y, Camera.NearPlane, Camera.FarPlane);
                    Camera.ProjectionMatrix = projectionMatrix;
                    GraphicsDevice.SetRenderTarget(renderTarget);
                    DrawSky(new DwarfTime(), Camera.ViewMatrix, 1.0f, Color.CornflowerBlue);
                    Draw3DThings(new DwarfTime(), DefaultShader, Camera.ViewMatrix);

                    DefaultShader.View = Camera.ViewMatrix;
                    DefaultShader.Projection = Camera.ProjectionMatrix;

                    ComponentRenderer.Render(renderables, new DwarfTime(), World.ChunkManager, Camera,
                        DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                        ComponentRenderer.WaterRenderType.None, 0);
                    InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera,
                        InstanceRenderMode.Normal);


                    GraphicsDevice.SetRenderTarget(null);
                    renderTarget.SaveAsPng(new FileStream(filename, FileMode.Create), resolution.X, resolution.Y);
                    GraphicsDevice.Textures[0] = null;
                    GraphicsDevice.Indices = null;
                    GraphicsDevice.SetVertexBuffer(null);
                    Camera.ProjectionMatrix = oldProjection;
                }
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public bool IsCameraUnderwater()
        {
            var handle = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Camera.Position + Vector3.Up));
            return handle.IsValid && handle.LiquidLevel > 0 && handle.Coordinate.Y <= (PersistentSettings.MaxViewingLevel >= World.WorldSizeInVoxels.Y ? 1000.0f : PersistentSettings.MaxViewingLevel + 0.25f);
        }

        private int _prevHour = 0;
        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Update(DwarfTime gameTime)
        {
            ValidateShader();

            FillClosestLights(gameTime);
            Camera.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
            Camera.Update(gameTime, World.ChunkManager);

            Sky.TimeOfDay = World.Time.GetSkyLightness();
            Sky.CosTime = (float)(World.Time.GetTotalHours() * 2 * Math.PI / 24.0f);
            DefaultShader.TimeOfDay = Sky.TimeOfDay;

            ChunkRenderer.Update(gameTime, Camera, GraphicsDevice);
        }

        public void ChangeCameraMode(OrbitCamera.ControlType type)
        {
            Camera.Control = type;
            if (type == OrbitCamera.ControlType.Walk)
            {
                SetMaxViewingLevel(World.WorldSizeInVoxels.Y);
                var below = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(new Vector3(Camera.Position.X, World.WorldSizeInVoxels.Y - 1, Camera.Position.Z))));
                Camera.Position = below.WorldPosition + Vector3.One * 0.5f + Vector3.Up;
            }
        }

        /// <summary>
        /// Reflects a camera beneath a water surface for reflection drawing TODO: move to water manager
        /// </summary>
        /// <param name="waterHeight">The height of the water (Y)</param>
        /// <returns>A reflection matrix</returns>
        public Matrix GetReflectedCameraMatrix(float waterHeight)
        {
            Vector3 reflCameraPosition = Camera.Position;
            reflCameraPosition.Y = -Camera.Position.Y + waterHeight * 2;
            Vector3 reflTargetPos = Camera.Target;
            reflTargetPos.Y = -Camera.Target.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Cross(Camera.Target - Camera.Position, Camera.UpVector);
            cameraRight.Normalize();
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);
            invUpVector.Normalize();
            return Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }

        /// <summary>
        /// Draws all the 3D terrain and entities
        /// </summary>
        /// <param name="gameTime">The current time</param>
        /// <param name="effect">The textured shader</param>
        /// <param name="view">The view matrix of the camera</param> 
        public void Draw3DThings(DwarfTime gameTime, Shader effect, Matrix view)
        {
            Matrix viewMatrix = Camera.ViewMatrix;
            Camera.ViewMatrix = view;

            GraphicsDevice.SamplerStates[0] = Drawer2D.PointMagLinearMin;
            effect.View = view;
            effect.Projection = Camera.ProjectionMatrix;
            effect.SetTexturedTechnique();
            effect.ClippingEnabled = true;
            effect.CaveView = CaveView;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            ChunkRenderer.Render(Camera, gameTime, GraphicsDevice, effect, Matrix.Identity);

            Camera.ViewMatrix = viewMatrix;
            effect.ClippingEnabled = true;
        }

        /// <summary>
        /// Draws the sky box
        /// </summary>
        /// <param name="time">The current time</param>
        /// <param name="view">The camera view matrix</param>
        /// <param name="scale">The scale for the sky drawing</param>
        /// <param name="fogColor"></param>
        /// <param name="drawBackground"></param>
        public void DrawSky(DwarfTime time, Matrix view, float scale, Color fogColor, bool drawBackground = true)
        {
            Matrix oldView = Camera.ViewMatrix;
            Camera.ViewMatrix = view;
            Sky.Render(time, GraphicsDevice, Camera, scale, fogColor, World.ChunkManager.Bounds, drawBackground);
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Camera.ViewMatrix = oldView;
        }

        public void RenderUninitialized(DwarfTime gameTime, String tip = null)
        {
            Render(gameTime);
        }

        public void FillClosestLights(DwarfTime time)
        {
            List<Vector3> positions = (from light in DynamicLight.Lights select light.Position).ToList();
            positions.AddRange((from light in DynamicLight.TempLights select light.Position));
            positions.Sort((a, b) =>
            {
                float dA = (a - Camera.Position).LengthSquared();
                float dB = (b - Camera.Position).LengthSquared();
                return dA.CompareTo(dB);
            });

            if (!GameSettings.Default.CursorLightEnabled)
            {
                LightPositions[0] = new Vector3(-99999, -99999, -99999);
            }
            else
            {
                LightPositions[0] = CursorLightPos;
            }

            int numLights = GameSettings.Default.CursorLightEnabled ? Math.Min(16, positions.Count + 1) : Math.Min(16, positions.Count);
            for (int i = GameSettings.Default.CursorLightEnabled ? 1 : 0; i < numLights; i++)
            {
                if (i > positions.Count)
                {
                    LightPositions[i] = new Vector3(-99999, -99999, -99999);
                }
                else
                {
                    LightPositions[i] = GameSettings.Default.CursorLightEnabled ? positions[i - 1] : positions[i];
                }
            }

            for (int j = numLights; j < 16; j++)
            {
                LightPositions[j] = new Vector3(0, 0, 0);
            }
            DefaultShader.CurrentNumLights = Math.Max(Math.Min(GameSettings.Default.CursorLightEnabled ? numLights - 1 : numLights, 15), 0);
            DynamicLight.TempLights.Clear();
        }

        public void ValidateShader()
        {
            if (DefaultShader == null || DefaultShader.IsDisposed || DefaultShader.GraphicsDevice.IsDisposed)
            {
                DefaultShader = new Shader(Content.Load<Effect>(ContentPaths.Shaders.TexturedShaders), true);
                DefaultShader.ScreenWidth = GraphicsDevice.Viewport.Width;
                DefaultShader.ScreenHeight = GraphicsDevice.Viewport.Height;
            }
        }

        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public void Render(DwarfTime gameTime)
        {
            if (!World.ShowingWorld)
            {
                return;
            }
            ValidateShader();
            var frustum = Camera.GetDrawFrustum();
            var renderables = World.EnumerateIntersectingObjects(frustum,
                r => r.IsVisible && !World.ChunkManager.IsAboveCullPlane(r.GetBoundingBox()));

            // Controls the sky fog
            float x = (1.0f - Sky.TimeOfDay);
            x = x * x;
            DefaultShader.FogColor = new Color(0.32f * x, 0.58f * x, 0.9f * x);
            DefaultShader.LightPositions = LightPositions;

            CompositeLibrary.Render(GraphicsDevice);
            CompositeLibrary.Update();

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            if (lastWaterHeight < 0) // Todo: Seriously, every single frame??
            {
                lastWaterHeight = 0;
                foreach (var chunk in World.ChunkManager.ChunkMap)
                    for (int y = 0; y < VoxelConstants.ChunkSizeY; y++)
                        if (chunk.Data.LiquidPresent[y] > 0)
                            lastWaterHeight = Math.Max(y + chunk.Origin.Y, lastWaterHeight);
            }

            // Computes the water height.
            float wHeight = WaterRenderer.GetVisibleWaterHeight(World.ChunkManager, Camera, GraphicsDevice.Viewport,
                lastWaterHeight);

            lastWaterHeight = wHeight;

            // Draw reflection/refraction images
            WaterRenderer.DrawReflectionMap(renderables, gameTime, World, wHeight - 0.1f,
                GetReflectedCameraMatrix(wHeight),
                DefaultShader, GraphicsDevice);


            #region Draw Selection Buffer.

            if (SelectionBuffer == null)
                SelectionBuffer = new SelectionBuffer(8, GraphicsDevice);

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            // Defines the current slice for the GPU
            var level = PersistentSettings.MaxViewingLevel >= World.WorldSizeInVoxels.Y ? 1000.0f : PersistentSettings.MaxViewingLevel + 0.25f;
            Plane slicePlane = WaterRenderer.CreatePlane(level, new Vector3(0, -1, 0), Camera.ViewMatrix, false);

            if (SelectionBuffer.Begin(GraphicsDevice))
            {
                // Draw the whole world, and make sure to handle slicing
                DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
                DefaultShader.ClippingEnabled = true;
                DefaultShader.View = Camera.ViewMatrix;
                DefaultShader.Projection = Camera.ProjectionMatrix;
                DefaultShader.World = Matrix.Identity;

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Chunks");
                ChunkRenderer.RenderSelectionBuffer(DefaultShader, GraphicsDevice, Camera.ViewMatrix);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Chunks");

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Components");
                ComponentRenderer.RenderSelectionBuffer(renderables, gameTime, World.ChunkManager, Camera,
                    DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Components");

                //GamePerformance.Instance.StartTrackPerformance("Render - Selection Buffer - Instances");
                InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera,
                    InstanceRenderMode.SelectionBuffer);
                //GamePerformance.Instance.StopTrackPerformance("Render - Selection Buffer - Instances");

                SelectionBuffer.End(GraphicsDevice);
            }


            #endregion



            // Start drawing the bloom effect
            if (GameSettings.Default.EnableGlow)
            {
                bloom.BeginDraw();
            }

            // Draw the sky
            GraphicsDevice.Clear(DefaultShader.FogColor);
            DrawSky(gameTime, Camera.ViewMatrix, 1.0f, DefaultShader.FogColor);



            DefaultShader.FogEnd = GameSettings.Default.ChunkDrawDistance;
            DefaultShader.FogStart = GameSettings.Default.ChunkDrawDistance * 0.8f;

            CaveView = CaveView * 0.9f + TargetCaveView * 0.1f;
            DefaultShader.WindDirection = World.Weather.CurrentWind;
            DefaultShader.WindForce = 0.0005f * (1.0f + (float)Math.Sin(World.Time.GetTotalSeconds() * 0.001f));
            // Draw the whole world, and make sure to handle slicing
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;
            //Blue ghost effect above the current slice.
            DefaultShader.GhostClippingEnabled = true;
            Draw3DThings(gameTime, DefaultShader, Camera.ViewMatrix);


            // Now we want to draw the water on top of everything else
            DefaultShader.ClippingEnabled = true;
            DefaultShader.GhostClippingEnabled = false;

            //ComponentManager.CollisionManager.DebugDraw();

            DefaultShader.View = Camera.ViewMatrix;
            DefaultShader.Projection = Camera.ProjectionMatrix;
            DefaultShader.GhostClippingEnabled = true;
            // Now draw all of the entities in the game
            DefaultShader.ClipPlane = new Vector4(slicePlane.Normal, slicePlane.D);
            DefaultShader.ClippingEnabled = true;

            // Render simple geometry (boxes, etc.)
            Drawer3D.Render(GraphicsDevice, DefaultShader, Camera, DesignationDrawer, World.PersistentData.Designations, World);

            DefaultShader.EnableShadows = false;

            DefaultShader.View = Camera.ViewMatrix;

            ComponentRenderer.Render(renderables, gameTime, World.ChunkManager,
                Camera,
                DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader,
                ComponentRenderer.WaterRenderType.None, lastWaterHeight);
            InstanceRenderer.Flush(GraphicsDevice, DefaultShader, Camera, InstanceRenderMode.Normal);

            if (World.UserInterface.CurrentToolMode == "BuildZone" // Todo: ??
                || World.UserInterface.CurrentToolMode == "BuildWall" ||
                World.UserInterface.CurrentToolMode == "BuildObject")
            {
                DefaultShader.View = Camera.ViewMatrix;
                DefaultShader.Projection = Camera.ProjectionMatrix;
                DefaultShader.SetTexturedTechnique();
                GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            }

            WaterRenderer.DrawWater(
                GraphicsDevice,
                (float)gameTime.TotalGameTime.TotalSeconds,
                DefaultShader,
                Camera.ViewMatrix,
                GetReflectedCameraMatrix(wHeight),
                Camera.ProjectionMatrix,
                new Vector3(0.1f, 0.0f, 0.1f),
                Camera,
                World.ChunkManager);
            World.ParticleManager.Render(World, GraphicsDevice);
            DefaultShader.ClippingEnabled = false;

            if (UseFXAA && fxaa == null)
            {
                fxaa = new FXAA();
                fxaa.Initialize();
            }

            if (GameSettings.Default.EnableGlow)
            {
                if (UseFXAA)
                {
                    fxaa.Begin(DwarfTime.LastTime);
                }
                bloom.DrawTarget = UseFXAA ? fxaa.RenderTarget : null;

                bloom.Draw(gameTime.ToRealTime());
                if (UseFXAA)
                    fxaa.End(DwarfTime.LastTime);
            }
            else if (UseFXAA)
            {
                fxaa.End(DwarfTime.LastTime);
            }

            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };


            if (Debugger.Switches.DrawSelectionBuffer)
                SelectionBuffer.DebugDraw(GraphicsDevice.Viewport.Bounds);

            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Drawer2D.PointMagLinearMin,
                    null, rasterizerState, null, Matrix.Identity);
                //DwarfGame.SpriteBatch.Draw(Shadows.ShadowTexture, Vector2.Zero, Color.White);
                if (IsCameraUnderwater())
                {
                    Drawer2D.FillRect(DwarfGame.SpriteBatch, GraphicsDevice.Viewport.Bounds, new Color(10, 40, 60, 200));
                }

                Drawer2D.Render(DwarfGame.SpriteBatch, Camera, GraphicsDevice.Viewport);

                IndicatorManager.Render(gameTime);
            }
            finally
            {
                try
                {
                    DwarfGame.SpriteBatch.End();
                }
                catch (Exception exception)
                {
                    DwarfGame.SpriteBatch = new SpriteBatch(GraphicsDevice);
                }
            }

            if (Debugger.Switches.DrawComposites)
            {
                Vector2 offset = Vector2.Zero;
                foreach (var composite in CompositeLibrary.Composites)
                {
                    offset = composite.Value.DebugDraw(DwarfGame.SpriteBatch, (int)offset.X, (int)offset.Y);
                }
            }

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle =
                DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            foreach (var module in World.UpdateSystems)
                module.Render(gameTime, World.ChunkManager, Camera, DwarfGame.SpriteBatch, GraphicsDevice, DefaultShader);

            lock (ScreenshotLock)
            {
                foreach (Screenshot shot in Screenshots)
                {
                    TakeScreenshot(shot.FileName, shot.Resolution);
                }

                Screenshots.Clear();
            }
        }




        /// <summary>
        /// Called when the GPU is getting new settings
        /// </summary>
        /// <param name="sender">The object requesting new device settings</param>
        /// <param name="e">The device settings that are getting set</param>
        public static void GraphicsPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            if (e == null)
            {
                Console.Error.WriteLine("Preparing device settings given null event args.");
                return;
            }

            if (e.GraphicsDeviceInformation == null)
            {
                Console.Error.WriteLine("Somehow, GraphicsDeviceInformation is null!");
                return;
            }

            PresentationParameters pp = e.GraphicsDeviceInformation.PresentationParameters;
            if (pp == null)
            {
                Console.Error.WriteLine("Presentation parameters invalid.");
                return;
            }

            GraphicsAdapter adapter = e.GraphicsDeviceInformation.Adapter;
            if (adapter == null)
            {
                Console.Error.WriteLine("Somehow, graphics adapter is null!");
                return;
            }

            if (adapter.CurrentDisplayMode == null)
            {
                Console.Error.WriteLine("Somehow, CurrentDisplayMode is null!");
                return;
            }

            SurfaceFormat format = adapter.CurrentDisplayMode.Format;

            if (MultiSamples > 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = MultiSamples;
            }
            else if (MultiSamples <= 0 && MultiSamples != pp.MultiSampleCount)
            {
                pp.MultiSampleCount = 0;
            }
        }

        public void Dispose()
        {
            bloom.Dispose();
            WaterRenderer.Dispose();
        }

        public void SetMaxViewingLevel(int level)
        {
            if (level == PersistentSettings.MaxViewingLevel)
                return;
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_click_voxel, 0.15f, (float)(level / (float)World.WorldSizeInVoxels.Y) - 0.5f);

            var oldLevel = PersistentSettings.MaxViewingLevel;

            PersistentSettings.MaxViewingLevel = Math.Max(Math.Min(level, World.WorldSizeInVoxels.Y), 1);

            foreach (var c in World.ChunkManager.ChunkMap)
            {
                var oldSliceIndex = oldLevel - 1 - c.Origin.Y;
                if (oldSliceIndex >= 0 && oldSliceIndex < VoxelConstants.ChunkSizeY) c.InvalidateSlice(oldSliceIndex);

                var newSliceIndex = PersistentSettings.MaxViewingLevel - 1 - c.Origin.Y;
                if (newSliceIndex >= 0 && newSliceIndex < VoxelConstants.ChunkSizeY) c.InvalidateSlice(newSliceIndex);
            }
        }
    }
}
