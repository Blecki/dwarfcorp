using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This class renders a skybox and sky elements (like the sun and moon) to the screen.
    /// </summary>
    /// 
    //TODO: MONOFIX
    public class SkyRenderer
    {
        private TextureCube SkyTexture;
        private TextureCube NightTexture;
        public Model SkyMesh;
        public Texture2D SkyGrad;
        public Effect SkyEffect;
        public float TimeOfDay;
        public float CosTime;
        public Texture2D MoonTexture;
        public Texture2D SunTexture;
        public Vector3 SunPosition;
        public Vector3 SunlightDir;
        public VertexBuffer BackgroundMesh;
        public IndexBuffer BackgroundIndex;
        public Effect BackgroundEffect;
        public List<Vector3> StarPositions;
        private bool Initalized = false;

        public SkyRenderer()
        {
        }

        public void CreateContent()
        {
            if (GameState.Game.GraphicsDevice.IsDisposed)
            {
                return;
            }

            Initalized = true;

            SkyTexture = GameState.Game.Content.Load<TextureCube>(AssetManager.ResolveContentPath(ContentPaths.Sky.day_sky));
            NightTexture = GameState.Game.Content.Load<TextureCube>(AssetManager.ResolveContentPath(ContentPaths.Sky.night_sky));
            SkyMesh = GameState.Game.Content.Load<Model>(AssetManager.ResolveContentPath("Models/sphere"));
            SkyEffect = GameState.Game.Content.Load<Effect>(ContentPaths.Shaders.SkySphere);
            SkyGrad = AssetManager.GetContentTexture(ContentPaths.Gradients.skygradient);
            SkyEffect.Parameters["SkyboxTexture"].SetValue(SkyTexture);
            SkyEffect.Parameters["TintTexture"].SetValue(SkyGrad);
            MoonTexture = AssetManager.GetContentTexture(ContentPaths.Sky.moon);
            SunTexture = AssetManager.GetContentTexture(ContentPaths.Sky.sun);
            TimeOfDay = 0.0f;
            CosTime = 0.0f;
            BackgroundEffect = GameState.Game.Content.Load<Effect>(ContentPaths.Shaders.Background);

            foreach (ModelMesh mesh in SkyMesh.Meshes)
                foreach (ModelMeshPart part in mesh.MeshParts)
                    part.Effect = SkyEffect;

            int numStars = 5000;
            StarPositions = new List<Vector3>();

            for (int i = 0; i < numStars; i++)
            {
                var star = MathFunctions.RandVector3Cube();
                star.Normalize();
                star *= 1000;
                StarPositions.Add(star);
            }
        }

        public void Render(DwarfTime time, GraphicsDevice device, Camera camera, float scale, Color fogColor, BoundingBox backgroundScale, bool drawBackground=true)
        {
            if (!Initalized)
                CreateContent();

            ValidateBuffers();
            device.DepthStencilState = DepthStencilState.None;
            RenderNightSky(time, device, camera);
            RenderStars(time, device, camera, device.Viewport);
            RenderDaySky(time, device, camera);
            RenderSunMoon(time, device, camera, device.Viewport, scale);
            device.DepthStencilState = DepthStencilState.None;
            if (drawBackground)
                RenderBackgroundMesh(device, camera, fogColor, backgroundScale);
        }

        private void CreateBackgroundMesh(GraphicsDevice Device, BoundingBox worldBounds)
        {
            int resolution = 4;
            int width = 512;
            int height = 512;
            int numVerts = (width * height) / resolution;
            BackgroundMesh = new VertexBuffer(Device, VertexPositionColor.VertexDeclaration, numVerts, BufferUsage.None);
            var verts = new VertexPositionColor[numVerts];
            var noise = new Perlin(MathFunctions.RandInt(0, 1000));
            var posCenter = new Vector2(width, height) * 0.5f;
            var extents = worldBounds.Extents();
            var scale = 32.0f;
            var offset = new Vector3(extents.X, 0, extents.Z) * 0.5f * scale - new Vector3(worldBounds.Center().X, worldBounds.Min.Y, worldBounds.Center().Z);
            var i = 0;

            for (int x = 0; x < width; x += resolution)
            {
                for (int y = 0; y < height; y += resolution)
                {
                    float dist = MathFunctions.Clamp((new Vector2(x, y) - posCenter).Length() * 0.1f, 0, 4);

                    float landHeight = (noise.Generate(x*0.01f, y*0.01f))*8.0f*dist;
                    verts[i].Position = new Vector3(((float)x)/width*extents.X * scale,
                        ((int)(landHeight / 5.0f)) * 5.0f, ((float)y) / height * extents.Z * scale) - offset;
                    if (worldBounds.Contains(verts[i].Position) == ContainmentType.Contains)
                        verts[i].Position = new Vector3(verts[i].Position.X, Math.Min(verts[i].Position.Y, worldBounds.Min.Y), verts[i].Position.Z);
                    i++;
                }
            }

            BackgroundMesh.SetData(verts);
            var indices = SetUpTerrainIndices(width / resolution, height / resolution);
            BackgroundIndex = new IndexBuffer(Device, typeof(int), indices.Length, BufferUsage.None);
            BackgroundIndex.SetData(indices);
        }

        private static int[] SetUpTerrainIndices(int width, int height)
        {
            var indices = new int[(width - 1) * (height - 1) * 6];
            var counter = 0;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int lowerLeft = x + y * width;
                    int lowerRight = (x + 1) + y * width;
                    int topLeft = x + (y + 1) * width;
                    int topRight = (x + 1) + (y + 1) * width;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        public void RenderBackgroundMesh(GraphicsDevice device, Camera camera, Color fogColor, BoundingBox scale)
        {
            if (BackgroundMesh == null || BackgroundMesh.IsDisposed || BackgroundMesh.GraphicsDevice.IsDisposed || BackgroundIndex == null || BackgroundIndex.IsDisposed || BackgroundIndex.GraphicsDevice.IsDisposed)
            {
                CreateBackgroundMesh(device, scale);
            }
            device.SetVertexBuffer(BackgroundMesh);
            device.Indices = BackgroundIndex;
            device.BlendState = BlendState.Opaque;
            Matrix rotOnly = camera.ViewMatrix;
            rotOnly.Translation = Vector3.Zero;
            BackgroundEffect.Parameters["View"].SetValue(camera.ViewMatrix);
            BackgroundEffect.Parameters["Projection"].SetValue(camera.ProjectionMatrix);
            BackgroundEffect.Parameters["World"].SetValue(Matrix.Identity);
            BackgroundEffect.Parameters["Fog"].SetValue(fogColor.ToVector4());
            device.DepthStencilState = DepthStencilState.Default;
            foreach (var pass in BackgroundEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, BackgroundIndex.IndexCount / 3);
            }
        }

        public void ValidateBuffers()
        {
            if (SkyEffect.IsDisposed || BackgroundEffect.IsDisposed || (BackgroundMesh != null && BackgroundMesh.IsDisposed) || SunTexture.IsDisposed || MoonTexture.IsDisposed || SkyTexture.IsDisposed)
            {
                CreateContent();
            }
        }

        public void RenderDaySky(DwarfTime time, GraphicsDevice device, Camera camera)
        {
            SkyEffect.Parameters["SkyboxTexture"].SetValue(SkyTexture);
            SkyEffect.Parameters["ViewMatrix"].SetValue(camera.ViewMatrix);
            SkyEffect.Parameters["ProjectionMatrix"].SetValue(camera.ProjectionMatrix);
            SkyEffect.Parameters["xTransparency"].SetValue(1.0f - (float) Math.Pow(TimeOfDay, 2));
            SkyEffect.Parameters["xRot"].SetValue(Matrix.CreateRotationY((float) time.TotalGameTime.TotalSeconds * 0.005f));
            SkyEffect.CurrentTechnique = SkyEffect.Techniques[0];
            SkyEffect.Parameters["xTint"].SetValue(TimeOfDay);

            foreach(ModelMesh mesh in SkyMesh.Meshes)
                mesh.Draw();
        }

        public void RenderNightSky(DwarfTime time, GraphicsDevice device, Camera camera)
        {
            SkyEffect.Parameters["SkyboxTexture"].SetValue(NightTexture);
            SkyEffect.Parameters["ViewMatrix"].SetValue(camera.ViewMatrix);
            SkyEffect.Parameters["ProjectionMatrix"].SetValue(camera.ProjectionMatrix);
            SkyEffect.Parameters["xTransparency"].SetValue(1.0f - TimeOfDay);
            SkyEffect.Parameters["xRot"].SetValue(Matrix.CreateRotationZ(-(CosTime + 0.5f * (float) Math.PI)));
            SkyEffect.Parameters["xTint"].SetValue(0.0f);
            SkyEffect.CurrentTechnique = SkyEffect.Techniques[0];

            foreach(ModelMesh mesh in SkyMesh.Meshes)
                mesh.Draw();
        }

        public void RenderStars(DwarfTime time, GraphicsDevice device, Camera camera, Viewport viewPort)
        {
            var rot = Matrix.CreateRotationZ((-CosTime + 0.5f * (float)Math.PI));

            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.Additive, Drawer2D.PointMagLinearMin, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                foreach (var star in StarPositions)
                {
                    var transformed = Vector3.Transform(star, rot);
                    transformed += camera.Position;

                    var cameraFrame = Vector3.Transform(transformed, camera.ViewMatrix);

                    var unproject = viewPort.Project(transformed, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

                    if (cameraFrame.Z > 0.999f)
                        Drawer2D.FillRect(DwarfGame.SpriteBatch, new Rectangle((int)unproject.X, (int)unproject.Y, 2, 2), Color.White);
                }
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }
        }

        public void RenderSunMoon(DwarfTime time, GraphicsDevice device, Camera camera, Viewport viewPort, float scale)
        {
            var rot = Matrix.CreateRotationZ((-CosTime + 0.5f * (float) Math.PI));
            SunPosition = new Vector3(1000, 100, 0);
            var moonPosition = new Vector3(-1000, 100, 0);
            SunPosition = Vector3.Transform(SunPosition, rot);
            moonPosition = Vector3.Transform(moonPosition, rot);
            SunPosition += camera.Position;
            moonPosition += camera.Position;


            Vector3 cameraFrameSun = Vector3.Transform(SunPosition, camera.ViewMatrix);
            Vector3 cameraFramMoon = Vector3.Transform(moonPosition, camera.ViewMatrix);


            Vector3 unProjectSun = viewPort.Project(SunPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 unProjectMoon = viewPort.Project(moonPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

                if (cameraFrameSun.Z > 0.999f)
                {
                    DwarfGame.SpriteBatch.Draw(SunTexture, new Vector2(unProjectSun.X - SunTexture.Width / 2 * scale, unProjectSun.Y - SunTexture.Height / 2 * scale), null, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
                }
                if (cameraFramMoon.Z > 0.999f)
                {
                    DwarfGame.SpriteBatch.Draw(MoonTexture, new Vector2(unProjectMoon.X - SunTexture.Width / 2 * scale, unProjectMoon.Y - SunTexture.Height / 2 * scale), null, Color.White, 0, Vector2.Zero, scale * 4, SpriteEffects.None, 0.0f);
                }
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }

            Vector3 sunDir = (camera.Position - SunPosition);
            sunDir.Normalize();
            SunlightDir = sunDir;
        }
    }

}