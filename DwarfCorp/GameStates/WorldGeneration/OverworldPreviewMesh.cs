using System.Collections.Generic;
using System.Linq;
using LibNoise;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class OverworldPreviewMesh : IDisposable
    {
        private GraphicsDevice Device { get { return GameState.Game.GraphicsDevice; } }

        public Texture2D IconTexture;

        public VertexBuffer LandMesh;
        public IndexBuffer LandIndex;
        public RawPrimitive TreePrimitive;
        public RawPrimitive BalloonPrimitive;

        private float HeightScale = 0.04f;

        public bool CreateIfNeeded(Overworld Overworld)
        {
            if (LandMesh == null || LandMesh.IsDisposed || LandMesh.GraphicsDevice.IsDisposed)
            {
                CreateMesh(Device, Overworld);
                return true;
            }

            return false;
        }

        private static int[] SetUpTerrainIndices(int width, int height)
        {
            var indices = new int[(width - 1) * (height - 1) * 6];
            int counter = 0;
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

        private void CreateMesh(GraphicsDevice Device, Overworld Overworld)
        {
            var numVerts = (Overworld.Width + 1) * (Overworld.Height + 1);
            LandMesh = new VertexBuffer(Device, VertexPositionNormalTexture.VertexDeclaration, numVerts, BufferUsage.None);
            var verts = new VertexPositionNormalTexture[numVerts];

            int i = 0;
            for (int x = 0; x <= Overworld.Width; x += 1)
            {
                for (int y = 0; y <= Overworld.Height; y += 1)
                {
                    var landHeight = Overworld.Map.Height((x < Overworld.Width) ? x : x - 1, (y < Overworld.Height) ? y : y - 1);
                    verts[i].Position = new Vector3((float)x / Overworld.Width, landHeight * HeightScale, (float)y / Overworld.Height);
                    verts[i].TextureCoordinate = new Vector2((float)x / Overworld.Width, (float)y / Overworld.Height);

                    var normal = new Vector3(
                        Overworld.Map.Height(MathFunctions.Clamp(x + 1, 0, Overworld.Width - 1), MathFunctions.Clamp(y, 0, Overworld.Height - 1)) - Overworld.Height,
                        1.0f,
                        Overworld.Map.Height(MathFunctions.Clamp(x, 0, Overworld.Width - 1), MathFunctions.Clamp(y + 1, 0, Overworld.Height - 1)) - Overworld.Height);
                    normal.Normalize();
                    verts[i].Normal = normal;

                    i++;
                }
            }

            LandMesh.SetData(verts);

            var indices = SetUpTerrainIndices((Overworld.Width + 1), (Overworld.Height + 1));
            LandIndex = new IndexBuffer(Device, typeof(int), indices.Length, BufferUsage.None);
            LandIndex.SetData(indices);

            // Create tree mesh.

            TreePrimitive = new RawPrimitive();
            if (IconTexture == null)
                IconTexture = AssetManager.GetContentTexture("GUI\\map_icons");
            var iconSheet = new SpriteSheet(IconTexture, 16, 16);

            for (int x = 0; x < Overworld.Width; x += 1)
                for (int y = 0; y < Overworld.Height; y += 1)
                {
                    if (!MathFunctions.RandEvent(0.05f)) continue;
                    var elevation = Overworld.Map.Height(x, y);
                    if (elevation <= Overworld.GenerationSettings.SeaLevel) continue;
                    var biome = Library.GetBiome(Overworld.Map.Map[x, y].Biome);
                    if (biome.Icon.X > 0 || biome.Icon.Y > 0)
                    {
                        var bounds = Vector4.Zero;
                        var uvs = iconSheet.GenerateTileUVs(biome.Icon, out bounds);
                        var angle = MathFunctions.Rand() * (float)System.Math.PI;

                        TreePrimitive.AddQuad(
                            Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                            * Matrix.CreateRotationY(angle)
                            * Matrix.CreateScale(2.0f / Overworld.Width)
                            * Matrix.CreateTranslation((float)x / Overworld.Width, elevation * HeightScale + 1.0f / Overworld.Width, (float)y / Overworld.Height),
                            Color.White, Color.White, uvs, bounds);

                        TreePrimitive.AddQuad(
                            Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                            * Matrix.CreateRotationY((float)System.Math.PI / 2)
                            * Matrix.CreateRotationY(angle)
                            * Matrix.CreateScale(2.0f / Overworld.Width)
                            * Matrix.CreateTranslation((float)x / Overworld.Width, elevation * HeightScale + 1.0f / Overworld.Width, (float)y / Overworld.Height),
                            Color.White, Color.White, uvs, bounds);
                    }
                }
        }

        public void CreatBalloonMesh(Overworld Overworld)
        {
            BalloonPrimitive = new RawPrimitive();
            if (IconTexture == null)
                IconTexture = AssetManager.GetContentTexture("GUI\\map_icons");
            var iconSheet = new SpriteSheet(IconTexture, 16, 16);
            var bounds = Vector4.Zero;
            var uvs = iconSheet.GenerateTileUVs(new Point(2, 0), out bounds);
            var angle = MathFunctions.Rand() * (float)System.Math.PI;

            BalloonPrimitive.AddQuad(
                Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                * Matrix.CreateScale(6.0f / Overworld.Width),
                Color.White, Color.White, uvs, bounds);

            BalloonPrimitive.AddQuad(
                Matrix.CreateRotationX(-(float)System.Math.PI / 2)
                * Matrix.CreateRotationY((float)System.Math.PI / 2)
                * Matrix.CreateScale(6.0f / Overworld.Width),
                Color.White, Color.White, uvs, bounds);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if (LandMesh != null)
                    LandMesh.Dispose();
                if (LandIndex != null)
                    LandIndex.Dispose();

                LandMesh = null;
                LandIndex = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~OverworldPreviewMesh()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}