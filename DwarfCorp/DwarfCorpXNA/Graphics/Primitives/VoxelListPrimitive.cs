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
    public class VoxelListPrimitive : GeometricPrimitive
    {
        private static Vector3[] FaceDeltas = null;

        protected void InitializeStatics()
        {
            if(FaceDeltas == null)
            {
                FaceDeltas = new Vector3[6];
                FaceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);
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

        protected static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            switch (face)
            {
                case BoxFace.Top:
                case BoxFace.Bottom:
                    return true;
                case BoxFace.Back:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopBackRight,
                        neighborRamp, RampType.TopFrontLeft, RampType.TopFrontRight);
                case BoxFace.Front:
                    return CheckRamps(myRamp, RampType.TopFrontLeft, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopBackRight);
                case BoxFace.Left:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopFrontLeft,
                        neighborRamp, RampType.TopBackRight, RampType.TopFrontRight);
                case BoxFace.Right:
                    return CheckRamps(myRamp, RampType.TopBackRight, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopFrontLeft);
                default:
                    return false;
            }
        }

        private static bool RampSet(RampType ToCheck, RampType For)
        {
            return (ToCheck & For) != 0;
        }

        private static bool CheckRamps(RampType A, RampType A1, RampType A2, RampType B, RampType B1, RampType B2)
        {
            return (!RampSet(A, A1) && RampSet(B, B1)) || (!RampSet(A, A2) && RampSet(B, B2));
        }

        protected static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }

        protected static bool IsFaceVisible(VoxelHandle voxel, VoxelHandle neighbor, BoxFace face)
        {
            return
                !neighbor.IsValid
                || (neighbor.IsExplored && neighbor.IsEmpty)
                || (neighbor.Type.IsTransparent && !voxel.Type.IsTransparent)
                || !neighbor.IsVisible
                || (
                    neighbor.Type.CanRamp
                    && neighbor.RampType != RampType.None
                    && IsSideFace(face)
                    && ShouldDrawFace(face, neighbor.RampType, voxel.RampType)
                );
        }

        public void InitializeFromChunk(VoxelChunk chunk)
        {
            if (chunk == null)
                return;

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

                RawPrimitive sliceGeometry = null;

                lock (chunk.Data.SliceCache)
                {
                    var cachedSlice = chunk.Data.SliceCache[y];
                    if (cachedSlice != null)
                    {
                        lightCache.Clear(); // If we skip a slice, nothing in the cache will be reused.
                        sliceStack.Add(cachedSlice);

                        if (GameSettings.Default.GrassMotes)
                            chunk.RebuildMoteLayerIfNull(y);

                        continue;
                    }

                    sliceGeometry = new RawPrimitive
                    {
                        Vertices = new ExtendedVertex[128],
                        Indexes = new ushort[128]
                    };
                    
                    chunk.Data.SliceCache[y] = sliceGeometry;
                }

                if (GameSettings.Default.CalculateRamps)
                    UpdateCornerRamps(chunk, y);
                    

                if (GameSettings.Default.GrassMotes)
                    chunk.RebuildMoteLayer(y);
                
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

                chunk.PrimitiveMutex.WaitOne();
                chunk.NewPrimitive = this;
                chunk.NewPrimitiveReceived = true;
                chunk.PrimitiveMutex.ReleaseMutex();
            }
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

        private static VoxelVertex[] TopVerticies = new VoxelVertex[]
            {
                VoxelVertex.FrontTopLeft,
                VoxelVertex.FrontTopRight,
                VoxelVertex.BackTopLeft,
                VoxelVertex.BackTopRight
            };
        
        private static void UpdateVoxelRamps(VoxelHandle V)
        {
            V.RampType = RampType.None;

            if (V.IsEmpty || !V.IsVisible || !V.Type.CanRamp)
                return;

            bool isTop = false;

            if (V.Coordinate.Y < VoxelConstants.ChunkSizeY - 1)
            {
                var vAbove = new VoxelHandle(V.Chunk, new LocalVoxelCoordinate(V.Coordinate.X, V.Coordinate.Y + 1, V.Coordinate.Z));
                isTop = vAbove.IsEmpty;
            }

            if (!isTop)
                return;

            foreach (var vertex in TopVerticies)
            {
                // If there are no empty neighbors, no slope.
                if (!VoxelHelpers.EnumerateVertexNeighbors2D(V.Coordinate, vertex)
                    .Any(n =>
                    {
                        var handle = new VoxelHandle(V.Chunk.Manager.ChunkData, n);
                        return !handle.IsValid || handle.IsEmpty;
                    }))
                    continue;

                switch (vertex)
                {
                    case VoxelVertex.FrontTopLeft:
                        V.RampType |= RampType.TopFrontLeft;
                        break;
                    case VoxelVertex.FrontTopRight:
                        V.RampType |= RampType.TopFrontRight;
                        break;
                    case VoxelVertex.BackTopLeft:
                        V.RampType |= RampType.TopBackLeft;
                        break;
                    case VoxelVertex.BackTopRight:
                        V.RampType |= RampType.TopBackRight;
                        break;
                }
            }
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
                    
                    v.RampType = RampType.None;

                    if (v.IsEmpty || !v.IsVisible || !v.Type.CanRamp)
                        continue;

                    bool isTop = false;

                    if (Y < VoxelConstants.ChunkSizeY - 1)
                    {
                        var vAbove = new VoxelHandle(Chunk, new LocalVoxelCoordinate(x, Y + 1, z));

                        isTop = vAbove.IsEmpty;
                    }

                    if (!isTop)
                        continue;

                    foreach (var vertex in top)
                    {
                        // If there are no empty neighbors, no slope.
                        if (!VoxelHelpers.EnumerateVertexNeighbors2D(v.Coordinate, vertex)
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
    }
}