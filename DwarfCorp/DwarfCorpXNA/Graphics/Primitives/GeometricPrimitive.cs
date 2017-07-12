using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;


namespace DwarfCorp
{
    /// <summary>
    /// Simple class representing a geometric object with verticies, textures, and whatever else.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GeometricPrimitive
    {
        public int MaxIndex = 0;
        public int MaxVertex = 0;

        [JsonIgnore]
        public IndexBuffer IndexBuffer = null;

        public ushort[] Indexes = null;

        public ExtendedVertex[] Vertices = null;

        [JsonIgnore]
        public static Vector3[] FaceDeltas = new Vector3[6];

        [JsonIgnore]
        public static List<Vector3>[] VertexNeighbors2D = new List<Vector3>[8];

        [JsonIgnore]
        public VertexBuffer VertexBuffer = null;

        [JsonIgnore]
        protected object VertexLock = new object();

        [JsonIgnore] public RenderTarget2D Lightmap = null;

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            ResetBuffer(GameState.Game.GraphicsDevice);
        }

        public static readonly bool[, ,] FaceDrawMap = new bool[6, (int)RampType.All + 1, (int)RampType.All + 1];

        public static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            if (face == BoxFace.Top || face == BoxFace.Bottom)
            {
                return true;
            }

            return FaceDrawMap[(int)face, (int)myRamp, (int)neighborRamp];
        }

        public static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }
        
        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void Render(GraphicsDevice device)
        {
            lock (VertexLock)
            {
#if MONOGAME_BUILD
                device.SamplerStates[0].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[1].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[2].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[3].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[4].Filter = TextureFilter.MinLinearMagPointMipLinear;
#endif
                if (Vertices == null || Vertices.Length < 3)
                {
                    return;
                }

                if (VertexBuffer == null ||  VertexBuffer.IsDisposed)
                {
                    ResetBuffer(device);
                }

                if (MaxVertex <= 0)
                {
                    MaxVertex = Vertices.Length;
                }

                if (MaxIndex <= 0 && Indexes != null)
                {
                    MaxIndex = Indexes.Length;
                }

                device.SetVertexBuffer(VertexBuffer);
                if (IndexBuffer != null)
                {
                    device.Indices = IndexBuffer;
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, MaxVertex, 0, MaxIndex / 3);
                }
                else if (Indexes == null || Indexes.Length == 0)
                {
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, MaxVertex/3);
                }
            }
        }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void RenderWireframe(GraphicsDevice device)
        {
            lock (VertexLock)
            {
                RasterizerState state = new RasterizerState();
                RasterizerState oldState = device.RasterizerState;
                state.FillMode = FillMode.WireFrame;
                device.RasterizerState = state;
                Render(device);
                device.RasterizerState = oldState;
            }
        }

        public virtual void GenerateLightmap(Rectangle textureBounds)
        {
            BoundingBox box = GenerateLightmapUVs();
            CreateLightMapTexture(box, textureBounds);
        }

        /// <summary>
        /// Generates a lightmap UV unwrapping, and provides new UV bounds.
        /// </summary>
        /// <returns></returns>
        public virtual BoundingBox GenerateLightmapUVs()
        {
            BoundingBox bounds = new BoundingBox(Vector3.Zero, Vector3.One);

            bool success = false;

            do
            {
                success = GenerateLightmapUVsInBounds(bounds);

                if (!success)
                {
                    bounds = new BoundingBox(bounds.Min, bounds.Max * 1.25f);
                }
            } 
            while (!success);
            return bounds;
        }


        /// <summary>
        /// Creates a light map texture for the given bounds, and original UV texture bounds.
        /// </summary>
        /// <param name="bounds">New bounds in UV space of the lightmap</param>
        /// <param name="textureBounds">The bounds of the texture used to draw the primitive</param>
        public virtual void CreateLightMapTexture(BoundingBox bounds, Rectangle textureBounds)
        {
            float widthScale = bounds.Max.X - bounds.Min.X;
            float heightScale = bounds.Max.Y - bounds.Min.Y;
            int newWidth = (int)Math.Ceiling((widthScale) * textureBounds.Width);
            int newHeight = (int)Math.Ceiling((heightScale) * textureBounds.Height);


            if (Lightmap == null || Lightmap.Width < newWidth || Lightmap.Height < newHeight)
            {
               Lightmap = new RenderTarget2D(GameState.Game.GraphicsDevice, newWidth, newHeight, false,
                    SurfaceFormat.Color, DepthFormat.None);
            }
            else
            {
                newWidth = Lightmap.Width;
                newHeight = Lightmap.Height;
            }
            widthScale = newWidth / ((float)textureBounds.Width);
            heightScale = newHeight / ((float)textureBounds.Height);
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vector2 lmc = Vertices[i].LightmapCoordinate;
                lmc.X /= widthScale;
                lmc.Y /= heightScale;
                Vertices[i].LightmapCoordinate = lmc;

                Vector4 lmb = Vertices[i].LightmapBounds;
                lmb.X /= widthScale;
                lmb.Y /= heightScale;
                lmb.Z /= widthScale;
                lmb.W /= heightScale;
                Vertices[i].LightmapBounds = lmb;
            }
        }

        /// <summary>
        /// Generates UV map for the model for a light map with given bounds.
        /// Returns false if the triangles could not fit within the bounds.
        /// Bounds Y coordinates will not be used.
        /// </summary>
        /// <param name="bounds">Bounding box in the image (0, 1 space)</param>
        public virtual bool GenerateLightmapUVsInBounds(BoundingBox bounds)
        {
            // The top left of the quad
            // to draw.
            float penX = 0;
            float penY = 0;
            // The maximum height of the last row.
            float drawnHeight = 0;

            // For each 4-vertex quad...
            for (int quad = 0; quad < MaxVertex / 4; quad++)
            {

                // Compute the bounds of the quad
                float minQuadUvx = float.MaxValue;
                float minQuadUvy = float.MaxValue;
                float maxQuadUvx = float.MinValue;
                float maxQuadUvy = float.MinValue;
                
                for (int vertex = 0; vertex < 4; vertex++)
                {
                    int index = quad * 4 + vertex;
                    minQuadUvx = Math.Min(minQuadUvx, Vertices[index].TextureCoordinate.X);
                    minQuadUvy = Math.Min(minQuadUvy, Vertices[index].TextureCoordinate.Y);
                    maxQuadUvx = Math.Max(maxQuadUvx, Vertices[index].TextureCoordinate.X);
                    maxQuadUvy = Math.Max(maxQuadUvy, Vertices[index].TextureCoordinate.Y);
                }

                float quadWidth = maxQuadUvx - minQuadUvx;
                float quadHeight = maxQuadUvy - minQuadUvy;

                // If the current quad isn't going to 
                // fit, go to the next row down.
                if (penX + quadWidth > bounds.Max.X)
                {
                    penY += drawnHeight;
                    penX = 0;
                    drawnHeight = 0;
                }

                // If the current quad won't fit because we're
                // outside of the lower bounds, there isn't enough
                // space to draw the quads, so return false.
                if (penY + quadHeight > bounds.Max.Y)
                {
                    return false;
                }

                // For each vertex, try to draw it to the UV bounds.
                for (int vertex = 0; vertex < 4; vertex++)
                {
                    int index = quad * 4 + vertex;
                    // The coordinate is whatever the pen coordinate was plus the original UV coordinate, minus the
                    // top left corner of the quad.
                    Vertices[index].LightmapCoordinate = new Vector2(penX + (Vertices[index].TextureCoordinate.X - minQuadUvx), 
                                                                     penY + (Vertices[index].TextureCoordinate.Y - minQuadUvy));
                    Vertices[index].LightmapBounds = new Vector4(penX + Vertices[index].TextureBounds.X - minQuadUvx,
                                                                 penY + Vertices[index].TextureBounds.Y - minQuadUvy,
                                                                 penX + Vertices[index].TextureBounds.Z - minQuadUvx,
                                                                 penY + Vertices[index].TextureBounds.W - minQuadUvy);
                }

                // Move the pen over if any of the vertexes were drawn.
                penX += quadWidth;
                drawnHeight = Math.Max(drawnHeight, quadHeight);
            }
            return true;
        }


        /// <summary>
        /// Resets the vertex buffer object from the verticies.
        /// <param Name="device">GPU to draw with.</param></summary>
        public virtual void ResetBuffer(GraphicsDevice device)
        {
            if(DwarfGame.ExitGame)
            {
                return;
            }

            lock (VertexLock)
            {
                if (VertexBuffer != null && !VertexBuffer.IsDisposed)
                {
                    VertexBuffer.Dispose();
                    VertexBuffer = null;
                }

                if (IndexBuffer != null && !IndexBuffer.IsDisposed)
                {
                    IndexBuffer.Dispose();
                    IndexBuffer = null;
                }

                if (MaxIndex <= 0 && Indexes != null)
                {
                    MaxIndex = Indexes.Length;
                }

                if (MaxVertex <= 0 && Vertices != null)
                {
                    MaxVertex = Vertices.Length;
                }



                if (Vertices != null)
                {
                    VertexBuffer newBuff = new VertexBuffer(device, ExtendedVertex.VertexDeclaration, Vertices.Length,
                        BufferUsage.WriteOnly);
                    newBuff.SetData(Vertices);
                    VertexBuffer = newBuff;
                }

                if (Indexes != null)
                {
                    IndexBuffer newIndexBuff = new IndexBuffer(device, typeof (ushort), Indexes.Length, BufferUsage.None);
                    newIndexBuff.SetData(Indexes);
                    IndexBuffer = newIndexBuff;
                }

            }

        }
    }

}