using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a collection of voxels with a surface mesh. Efficiently culls away
    /// invisible voxels, and properly constructs ramps.
    /// </summary>
    public class VoxelListPrimitive : GeometricPrimitive, IDisposable
    {
        protected bool isRebuilding = false;
        private readonly Mutex rebuildMutex = new Mutex();
        private static bool StaticsInitialized = false;

        protected void InitializeStatics()
        {
            if(!StaticsInitialized)
            {
                FaceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);

                StaticsInitialized = true;
            }
        }

        public VoxelListPrimitive() :
            base()
        {
            InitializeStatics();
        }


        public static bool IsTopVertex(VoxelVertex v)
        {
            return v == VoxelVertex.BackTopLeft || v == VoxelVertex.FrontTopLeft || v == VoxelVertex.FrontTopRight || v == VoxelVertex.BackTopRight;
        }

        public void InitializeFromChunk(VoxelChunk chunk)
        {
            if (chunk == null)
                return;

            rebuildMutex.WaitOne();

            if (isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();

            int[] ambientValues = new int[4];
            VertexCount = 0;
            IndexCount = 0;
            BoxPrimitive bedrockModel = VoxelLibrary.GetPrimitive("Bedrock");
            var sliceStack = new List<RawPrimitive>();
            var totalBuilt = 0;
            var lightCache = new Dictionary<GlobalVoxelCoordinate, VertexColorInfo>();

            for (var y = 0; y < chunk.Manager.ChunkData.MaxViewingLevel; ++y)
            {
                if (chunk.Data.VoxelsPresentInSlice[y] == 0)
                {
                    lightCache.Clear(); // If we skip a slice, nothing in the cache will be reused.
                    continue;
                }

                if (chunk.Data.SliceCache[y] != null)
                {
                    lightCache.Clear(); // If we skip a slice, nothing in the cache will be reused.
                    sliceStack.Add(chunk.Data.SliceCache[y]);

                    if (GameSettings.Default.GrassMotes)
                        chunk.RebuildMoteLayerIfNull(y);

                    continue;
                }

                if (GameSettings.Default.CalculateRamps)
                    UpdateCornerRamps(chunk, y);

                if (GameSettings.Default.GrassMotes)
                    chunk.RebuildMoteLayer(y);

                var sliceGeometry = new RawPrimitive
                {
                    Vertices = new ExtendedVertex[128],
                    Indexes = new ushort[128]
                };

                chunk.Data.SliceCache[y] = sliceGeometry;

                for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                {
                    for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    {
                        BuildVoxelGeometry(ref sliceGeometry.Vertices, ref sliceGeometry.Indexes, ref sliceGeometry.VertexCount, ref sliceGeometry.IndexCount,
                            x, y, z, chunk, bedrockModel, ambientValues, lightCache);
                    }
                }

                sliceStack.Add(sliceGeometry);
                totalBuilt += 1;
            }

            if (totalBuilt > 0)
            {
                var combinedGeometry = RawPrimitive.Concat(sliceStack);

                Vertices = combinedGeometry.Vertices;
                VertexCount = combinedGeometry.VertexCount;
                Indexes = combinedGeometry.Indexes;
                IndexCount = combinedGeometry.IndexCount;

                GenerateLightmap(chunk.Manager.ChunkData.Tilemap.Bounds);

                chunk.PrimitiveMutex.WaitOne();
                chunk.NewPrimitive = this;
                chunk.NewPrimitiveReceived = true;
                chunk.PrimitiveMutex.ReleaseMutex();
            }

            isRebuilding = false;
        }

        private static GlobalVoxelCoordinate GetCacheKey(VoxelHandle Handle, VoxelVertex Vertex)
        {
            var coord = Handle.Coordinate;

            if ((Vertex & VoxelVertex.Front) == VoxelVertex.Front)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y, coord.Z + 1);

            if ((Vertex & VoxelVertex.Top) == VoxelVertex.Top)
                coord = new GlobalVoxelCoordinate(coord.X, coord.Y + 1, coord.Z);

            if ((Vertex & VoxelVertex.Right) == VoxelVertex.Right)
                coord = new GlobalVoxelCoordinate(coord.X + 1, coord.Y, coord.Z);

            return coord;
        }

        private static void BuildVoxelGeometry(
            ref ExtendedVertex[] Verticies,
            ref ushort[] Indicies,
            ref int VertexCount,
            ref int IndexCount,
            int X,
            int Y,
            int Z,
            VoxelChunk Chunk,
            BoxPrimitive BedrockModel,
            int[] AmbientScratchSpace,
            Dictionary<GlobalVoxelCoordinate, VertexColorInfo> LightCache)
        {
            var v = new VoxelHandle(Chunk, new LocalVoxelCoordinate(X, Y, Z));

            if ((v.IsExplored && v.IsEmpty) || !v.IsVisible) return;

            var primitive = VoxelLibrary.GetPrimitive(v.Type);
            if (v.IsExplored && primitive == null) return;

            if (!v.IsExplored)
                primitive = BedrockModel;

            var tint = v.Type.Tint;

            var uvs = primitive.UVs;

            if (v.Type.HasTransitionTextures && v.IsExplored)
            {
                uvs = ComputeTransitionTexture(new VoxelHandle(v.Chunk.Manager.ChunkData, v.Coordinate));
            }

            for (int i = 0; i < 6; i++)
            {
                BoxFace face = (BoxFace)i;
                Vector3 delta = FaceDeltas[i];

                var faceVoxel = new VoxelHandle(Chunk.Manager.ChunkData,
                        Chunk.ID + new LocalVoxelCoordinate(X + (int)delta.X, Y + (int)delta.Y, Z + (int)delta.Z));

                if (!IsFaceVisible(v, faceVoxel, face))
                    continue;

                var faceDescriptor = primitive.GetFace(face);
                var indexOffset = VertexCount;

                for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
                {
                    var vertex = primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];
                    var voxelVertex = primitive.Deltas[faceDescriptor.VertexOffset + faceVertex];

                    var cacheKey = GetCacheKey(v, voxelVertex);
                    VertexColorInfo vertexColor;
                    if (!LightCache.TryGetValue(cacheKey, out vertexColor))
                    {
                        vertexColor = CalculateVertexLight(v, voxelVertex, Chunk.Manager);
                        LightCache.Add(cacheKey, vertexColor);
                    }

                    AmbientScratchSpace[faceVertex] = vertexColor.AmbientColor;

                    var rampOffset = Vector3.Zero;
                    if (v.Type.CanRamp && ShouldRamp(voxelVertex, v.RampType))
                        rampOffset = new Vector3(0, -v.Type.RampSize, 0);

                    EnsureSpace(ref Verticies, VertexCount);

                    var worldPosition = v.WorldPosition + vertex.Position;

                    Verticies[VertexCount] = new ExtendedVertex(
                        worldPosition + rampOffset + VertexNoise.GetNoiseVectorFromRepeatingTexture(worldPosition),
                        vertexColor.AsColor(),
                        tint,
                        uvs.Uvs[faceDescriptor.VertexOffset + faceVertex],
                        uvs.Bounds[faceDescriptor.IndexOffset / 6]);

                    VertexCount++;
                }

                bool flippedQuad = AmbientScratchSpace[0] + AmbientScratchSpace[2] >
                                   AmbientScratchSpace[1] + AmbientScratchSpace[3];

                for (int idx = faceDescriptor.IndexOffset; idx < faceDescriptor.IndexCount +
                    faceDescriptor.IndexOffset; idx++)
                {
                    EnsureSpace(ref Indicies, IndexCount);

                    ushort offset = flippedQuad ? primitive.FlippedIndexes[idx] : primitive.Indexes[idx];
                    ushort offset0 = flippedQuad ? primitive.FlippedIndexes[faceDescriptor.IndexOffset] : primitive.Indexes[faceDescriptor.IndexOffset];
                    Indicies[IndexCount] = (ushort)(indexOffset + offset - offset0);
                    IndexCount++;
                }
            }
            // End looping faces
        }

        private static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if ((rampType & RampType.TopFrontRight) == RampType.TopFrontRight)
                toReturn = (vertex == VoxelVertex.FrontTopRight);

            if ((rampType & RampType.TopBackRight) == RampType.TopBackRight)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopRight);

            if ((rampType & RampType.TopFrontLeft) == RampType.TopFrontLeft)
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);

            if ((rampType & RampType.TopBackLeft) == RampType.TopBackLeft)
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);

            return toReturn;
        }


        public static void UpdateCornerRamps(VoxelChunk Chunk, int Y)
        {
            List<VoxelVertex> top = new List<VoxelVertex>()
            {
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight,
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight
            };

            for (int x = 0; x < VoxelConstants.ChunkSizeX; x++)
            {
                for (int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                {
                    var v = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, Y, z));
                    bool isTop = false;

                    if (Y < VoxelConstants.ChunkSizeY - 1)
                    {
                        var vAbove = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, Y + 1, z));

                        isTop = vAbove.IsEmpty;
                    }

                    v.RampType = RampType.None;

                    // Check solid voxels
                    if (v.WaterCell.Type == LiquidType.None)
                    {
                        if (v.IsEmpty || !v.IsVisible || !isTop || !v.Type.CanRamp)
                            continue;
                    }
                    // Check liquid voxels for tops
                    else
                    {
                        if (!isTop)
                            continue;
                    }

                    foreach (var vertex in top)
                    {
                        // If there are no empty neighbors, no slope.
                        if (v.WaterCell.Type == LiquidType.None
                            && !VoxelHelpers.EnumerateVertexNeighbors2D(v.Coordinate, vertex)
                            .Any(n =>
                            {
                                var handle = new VoxelHandle(Chunk.Manager.ChunkData, n);
                                return !handle.IsValid || handle.IsEmpty;
                            }))
                            continue;

                        switch (vertex)
                        {
                            case VoxelVertex.FrontTopLeft:
                                v.RampType |= RampType.TopFrontLeft;
                                break;
                            case VoxelVertex.FrontTopRight:
                                v.RampType |= RampType.TopFrontRight;
                                break;
                            case VoxelVertex.BackTopLeft:
                                v.RampType |= RampType.TopBackLeft;
                                break;
                            case VoxelVertex.BackTopRight:
                                v.RampType |= RampType.TopBackRight;
                                break;
                        }
                    }
                }
            }
        }

        private static void EnsureSpace<T>(ref T[] In, int Size)
        {
            if (Size >= In.Length)
            {
                var r = new T[In.Length * 2];
                In.CopyTo(r, 0);
                In = r;
            }
        }

        private struct VertexColorInfo
        {
            public int SunColor;
            public int AmbientColor;
            public int DynamicColor;

            public Color AsColor()
            {
                return new Color(SunColor, AmbientColor, DynamicColor);
            }
        }

        private static VertexColorInfo CalculateVertexLight(VoxelHandle Vox, VoxelVertex Vertex,
            ChunkManager chunks)
        {
            int neighborsEmpty = 0;
            int neighborsChecked = 0;

            var color = new VertexColorInfo();
            color.DynamicColor = 0;
            color.SunColor = 0;

            foreach (var c in VoxelHelpers.EnumerateVertexNeighbors(Vox.Coordinate, Vertex))
            {
                var v = new VoxelHandle(chunks.ChunkData, c);
                if (!v.IsValid) continue;

                color.SunColor += v.SunColor;
                if (!v.IsEmpty || !v.IsExplored)
                {
                    if (v.Type.EmitsLight) color.DynamicColor = 255;
                    neighborsEmpty += 1;
                    neighborsChecked += 1;
                }
                else
                    neighborsChecked += 1;
            }

            float proportionHit = (float)neighborsEmpty / (float)neighborsChecked;
            color.AmbientColor = (int)Math.Min((1.0f - proportionHit) * 255.0f, 255);
            color.SunColor = (int)Math.Min((float)color.SunColor / (float)neighborsChecked, 255);

            return color;
        }

        private static BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(VoxelHandle V)
        {
            var type = V.Type;
            var primitive = VoxelLibrary.GetPrimitive(type);

            if (!type.HasTransitionTextures && primitive != null)
                return primitive.UVs;
            else if (primitive == null)
                return null;
            else
            {
                var transition = ComputeTransitions(V.Chunk.Manager.ChunkData, V, type);
                return type.TransitionTextures[transition];
            }
        }

        private static BoxTransition ComputeTransitions(
            ChunkData Data,
            VoxelHandle V,
            VoxelType Type)
        {
            if (Type.Transitions == VoxelType.TransitionType.Horizontal)
            {
                var value = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate)
                    .Select(c => new VoxelHandle(Data, c)), Type);

                return new BoxTransition()
                {
                    Top = (TransitionTexture)value
                };
            }
            else
            {
                var transitionFrontBack = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.Z)
                    .Select(c => new VoxelHandle(Data, c)),
                    Type);

                var transitionLeftRight = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.X)
                    .Select(c => new VoxelHandle(Data, c)),
                    Type);

                return new BoxTransition()
                {
                    Front = (TransitionTexture)transitionFrontBack,
                    Back = (TransitionTexture)transitionFrontBack,
                    Left = (TransitionTexture)transitionLeftRight,
                    Right = (TransitionTexture)transitionLeftRight
                };
            }
        }

        // Todo: Reorder 2d neighbors to make this unecessary.
        private static int[] TransitionMultipliers = new int[] { 2, 8, 4, 1 };

        private static int ComputeTransitionValueOnPlane(IEnumerable<VoxelHandle> Neighbors, VoxelType Type)
        {
            int index = 0;
            int accumulator = 0;
            foreach (var v in Neighbors)
            {
                if (v.IsValid && !v.IsEmpty && v.Type == Type)
                    accumulator += TransitionMultipliers[index];
                index += 1;
            }
            return accumulator;
        }

        public void Dispose()
        {
            rebuildMutex.Dispose();
        }

        private void GenerateLightmap(Rectangle textureBounds)
        {
            if (GameSettings.Default.UseLightmaps)
            {
                BoundingBox box = GenerateLightmapUVs();
                CreateLightMapTexture(box, textureBounds);
            }
        }

        /// <summary>
        /// Generates a lightmap UV unwrapping, and provides new UV bounds.
        /// </summary>
        /// <returns></returns>
        private BoundingBox GenerateLightmapUVs()
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
        private void CreateLightMapTexture(BoundingBox bounds, Rectangle textureBounds)
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
        private bool GenerateLightmapUVsInBounds(BoundingBox bounds)
        {
            // The top left of the quad
            // to draw.
            float penX = 0;
            float penY = 0;
            // The maximum height of the last row.
            float drawnHeight = 0;

            // For each 4-vertex quad...
            for (int quad = 0; quad < VertexCount / 4; quad++)
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

    }

}