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

        private static readonly bool[, ,] FaceDrawMap = new bool[6, (int)RampType.All + 1, (int)RampType.All + 1];

        private static void SetTop(RampType rampType, Dictionary<VoxelVertex, float> top)
        {
            float r = 0.5f;
            List<VoxelVertex> keys = top.Keys.ToList();
            foreach (VoxelVertex vert in keys)
            {
                top[vert] = 1.0f;
            }
            if (rampType.HasFlag(RampType.TopFrontLeft))
            {
                top[VoxelVertex.FrontTopLeft] = r;
            }

            if (rampType.HasFlag(RampType.TopFrontRight))
            {
                top[VoxelVertex.FrontTopRight] = r;
            }

            if (rampType.HasFlag(RampType.TopBackLeft))
            {
                top[VoxelVertex.BackTopLeft] = r;
            }
            if (rampType.HasFlag(RampType.TopBackRight))
            {
                top[VoxelVertex.BackTopRight] = r;
            }
        }

        public static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            if (face == BoxFace.Top || face == BoxFace.Bottom)
            {
                return true;
            }

            return FaceDrawMap[(int)face, (int)myRamp, (int)neighborRamp];
        }

        public static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if (VoxelHandle.HasFlag(rampType, RampType.TopFrontRight))
            {
                toReturn = (vertex == VoxelVertex.BackTopRight);
            }

            if (VoxelHandle.HasFlag(rampType, RampType.TopBackRight))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopRight);
            }

            if (VoxelHandle.HasFlag(rampType, RampType.TopFrontLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);
            }

            if (VoxelHandle.HasFlag(rampType, RampType.TopBackLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);
            }


            return toReturn;
        }

        public static RampType UpdateRampType(BoxFace face)
        {
            switch (face)
            {
                case BoxFace.Back:
                    return RampType.Back;

                case BoxFace.Front:
                    return RampType.Front;

                case BoxFace.Left:
                    return RampType.Left;

                case BoxFace.Right:
                    return RampType.Right;

                default:
                    return RampType.None;
            }
        }

        public static bool RampIsDegenerate(RampType rampType)
        {
            return rampType == RampType.All
                   || (VoxelHandle.HasFlag(rampType, RampType.Left) && VoxelHandle.HasFlag(rampType, RampType.Right))
                   || (VoxelHandle.HasFlag(rampType, RampType.Front) && VoxelHandle.HasFlag(rampType, RampType.Back));
        }

        public static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }


        public static void UpdateCornerRamps(VoxelChunk chunk)
        {
            var v = chunk.MakeVoxel(0, 0, 0);
            var vAbove = chunk.MakeVoxel(0, 0, 0);
            List<VoxelHandle> diagNeighbors = chunk.AllocateVoxels(3);
            List<VoxelVertex> top = new List<VoxelVertex>()
            {
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight,
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight
            };

            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int y = 0; y < chunk.SizeY; y++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z);
                        bool isTop = false;


                        if (y < chunk.SizeY - 1)
                        {
                            vAbove.GridPosition = new Vector3(x, y + 1, z);

                            isTop = vAbove.IsEmpty;
                        }

                        if (v.IsEmpty || !v.IsVisible || !isTop || !v.Type.CanRamp)
                        {
                            v.RampType = RampType.None;
                            continue;
                        }
                        v.RampType = RampType.None;

                        foreach (VoxelVertex bestKey in top)
                        {
                            List<Vector3> neighbors = VertexNeighbors2D[(int)bestKey];
                            chunk.GetNeighborsSuccessors(neighbors, (int)v.GridPosition.X, (int)v.GridPosition.Y, (int)v.GridPosition.Z, diagNeighbors);

                            bool emptyFound = diagNeighbors.Any(vox => vox == null || vox.IsEmpty);

                            if (!emptyFound)
                            {
                                continue;
                            }

                            switch (bestKey)
                            {
                                case VoxelVertex.FrontTopLeft:
                                    v.RampType |= RampType.TopBackLeft;
                                    break;
                                case VoxelVertex.FrontTopRight:
                                    v.RampType |= RampType.TopBackRight;
                                    break;
                                case VoxelVertex.BackTopLeft:
                                    v.RampType |= RampType.TopFrontLeft;
                                    break;
                                case VoxelVertex.BackTopRight:
                                    v.RampType |= RampType.TopFrontRight;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // This insanity creates a map from voxel faces to ramp types, determining
        // whether or not the voxel on a face should draw that face given that its neighbor
        // has a certain ramp type.
        // This allows faces to be drawn next to ramps (for example, if dirt is next to stone,
        // the dirt ramps, revealing part of the stone face that needs to be drawn).
        protected static void CreateFaceDrawMap()
        {
            BoxFace[] faces = (BoxFace[])Enum.GetValues(typeof(BoxFace));

            List<RampType> ramps = new List<RampType>()
            {
                RampType.None,
                RampType.TopFrontLeft,
                RampType.TopFrontRight,
                RampType.TopBackRight,
                RampType.TopBackLeft,
                RampType.Front,
                RampType.All,
                RampType.Back,
                RampType.Left,
                RampType.Right,
                RampType.TopFrontLeft | RampType.TopBackRight,
                RampType.TopFrontRight | RampType.TopBackLeft,
                RampType.TopBackLeft | RampType.TopBackRight | RampType.TopFrontLeft,
                RampType.TopBackLeft | RampType.TopBackRight | RampType.TopFrontRight,
                RampType.TopFrontLeft | RampType.TopFrontRight | RampType.TopBackLeft,
                RampType.TopFrontLeft | RampType.TopFrontRight | RampType.TopBackRight
            };

            Dictionary<VoxelVertex, float> myTop = new Dictionary<VoxelVertex, float>()
            {
                {
                    VoxelVertex.BackTopLeft, 0.0f
                },
                {
                    VoxelVertex.BackTopRight,  0.0f
                },
                {
                    VoxelVertex.FrontTopLeft,  0.0f
                },
                {
                    VoxelVertex.FrontTopRight,  0.0f
                }
            };

            Dictionary<VoxelVertex, float> theirTop = new Dictionary<VoxelVertex, float>()
            {
                {
                    VoxelVertex.BackTopLeft,  0.0f
                },
                {
                    VoxelVertex.BackTopRight,  0.0f
                },
                {
                    VoxelVertex.FrontTopLeft,  0.0f
                },
                {
                    VoxelVertex.FrontTopRight, 0.0f
                }
            };

            foreach (RampType myRamp in ramps)
            {
                SetTop(myRamp, myTop);
                foreach (RampType neighborRamp in ramps)
                {
                    SetTop(neighborRamp, theirTop);
                    foreach (BoxFace neighborFace in faces)
                    {

                        if (neighborFace == BoxFace.Bottom || neighborFace == BoxFace.Top)
                        {
                            FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = false;
                            continue;
                        }

                        float my1 = 0.0f;
                        float my2 = 0.0f;
                        float their1 = 0.0f;
                        float their2 = 0.0f;

                        switch (neighborFace)
                        {
                            case BoxFace.Back:
                                my1 = myTop[VoxelVertex.BackTopLeft];
                                my2 = myTop[VoxelVertex.BackTopRight];
                                their1 = theirTop[VoxelVertex.FrontTopLeft];
                                their2 = theirTop[VoxelVertex.FrontTopRight];
                                break;
                            case BoxFace.Front:
                                my1 = myTop[VoxelVertex.FrontTopLeft];
                                my2 = myTop[VoxelVertex.FrontTopRight];
                                their1 = theirTop[VoxelVertex.BackTopLeft];
                                their2 = theirTop[VoxelVertex.BackTopRight];
                                break;
                            case BoxFace.Left:
                                my1 = myTop[VoxelVertex.FrontTopLeft];
                                my2 = myTop[VoxelVertex.BackTopLeft];
                                their1 = theirTop[VoxelVertex.FrontTopRight];
                                their2 = theirTop[VoxelVertex.BackTopRight];
                                break;
                            case BoxFace.Right:
                                my1 = myTop[VoxelVertex.FrontTopRight];
                                my2 = myTop[VoxelVertex.BackTopRight];
                                their1 = theirTop[VoxelVertex.FrontTopLeft];
                                their2 = theirTop[VoxelVertex.BackTopLeft];
                                break;
                            default:
                                break;
                        }

                        FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = (their1 < my1 || their2 < my2);
                    }
                }
            }
        }

        public static void UpdateRamps(VoxelChunk chunk)
        {
            Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
            Dictionary<BoxFace, bool> faceVisible = new Dictionary<BoxFace, bool>();
            var v = chunk.MakeVoxel(0, 0, 0);
            var vAbove = chunk.MakeVoxel(0, 0, 0);
            var voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            var worldVoxel = new VoxelHandle();

            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z);
                        bool isTop = false;


                        if (y < chunk.SizeY - 1)
                        {
                            vAbove.GridPosition = new Vector3(x, y + 1, z);

                            isTop = vAbove.IsEmpty;
                        }


                        if (isTop && !v.IsEmpty && v.IsVisible && v.Type.CanRamp)
                        {

                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                if (!IsSideFace(face))
                                {
                                    continue;
                                }

                                Vector3 delta = FaceDeltas[(int)face];
                                faceExists[face] = chunk.IsCellValid(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z);
                                faceVisible[face] = true;

                                if (faceExists[face])
                                {
                                    voxelOnFace.GridPosition = new Vector3(x + (int)delta.X, y + (int)delta.Y,
                                        z + (int)delta.Z);

                                    if (voxelOnFace.IsEmpty || !voxelOnFace.IsVisible)
                                    {
                                        faceVisible[face] = true;
                                    }
                                    else
                                    {
                                        faceVisible[face] = false;
                                    }
                                }
                                else
                                {
                                    if (!chunk.Manager.ChunkData.GetNonNullVoxelAtWorldLocation(new Vector3(x + (int)delta.X + 0.5f, y + (int)delta.Y + 0.5f, z + (int)delta.Z + 0.5f) + chunk.Origin, ref worldVoxel) || !worldVoxel.IsVisible)
                                    {
                                        faceVisible[face] = true;
                                    }
                                    else
                                    {
                                        faceVisible[face] = false;
                                    }
                                }


                                if (faceVisible[face])
                                {
                                    v.RampType = v.RampType | UpdateRampType(face);
                                }
                            }

                            if (RampIsDegenerate(v.RampType))
                            {
                                v.RampType = RampType.None;
                            }
                        }
                        else if (!v.IsEmpty && v.IsVisible && v.Type.CanRamp)
                        {
                            v.RampType = RampType.None;
                        }
                    }
                }
            }
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