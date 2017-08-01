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

            for (var y = 0; y < chunk.Manager.ChunkData.MaxViewingLevel; ++y)
            {
                if (chunk.Data.VoxelsPresentInSlice[y] == 0) continue;
                if (chunk.Data.SliceCache[y] != null)
                {
                    sliceStack.Add(chunk.Data.SliceCache[y]);
                    continue;
                }

                if (GameSettings.Default.CalculateRamps)
                    UpdateCornerRamps(chunk, y);

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
                            x, y, z, chunk, bedrockModel, ambientValues);
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
            int[] AmbientScratchSpace)
        {
            var v = new TemporaryVoxelHandle(Chunk, new LocalVoxelCoordinate(X, Y, Z));

            if ((v.IsExplored && v.IsEmpty) || !v.IsVisible) return;

            var primitive = VoxelLibrary.GetPrimitive(v.Type);
            if (v.IsExplored && primitive == null) return;

            if (!v.IsExplored)
                primitive = BedrockModel;

            var tint = v.Type.Tint;

            var uvs = primitive.UVs;

            if (v.Type.HasTransitionTextures && v.IsExplored)
            {
                uvs = ComputeTransitionTexture(new TemporaryVoxelHandle(v.Chunk.Manager.ChunkData, v.Coordinate));
            }

            for (int i = 0; i < 6; i++)
            {
                BoxFace face = (BoxFace)i;
                Vector3 delta = FaceDeltas[i];

                var faceVoxel = new TemporaryVoxelHandle(Chunk.Manager.ChunkData,
                        Chunk.ID + new LocalVoxelCoordinate(X + (int)delta.X, Y + (int)delta.Y, Z + (int)delta.Z));

                if (!IsFaceVisible(v, faceVoxel, face))
                    continue;

                var faceDescriptor = primitive.GetFace(face);
                var indexOffset = VertexCount;

                for (int faceVertex = 0; faceVertex < faceDescriptor.VertexCount; faceVertex++)
                {
                    var vertex = primitive.Vertices[faceDescriptor.VertexOffset + faceVertex];
                    var voxelVertex = primitive.Deltas[faceDescriptor.VertexOffset + faceVertex];

                    var vertexColor = CalculateVertexLight(v, voxelVertex, Chunk.Manager);
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
            {
                toReturn = (vertex == VoxelVertex.FrontTopRight);
            }

            if ((rampType & RampType.TopBackRight) == RampType.TopBackRight)
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopRight);
            }

            if ((rampType & RampType.TopFrontLeft) == RampType.TopFrontLeft)
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);
            }

            if ((rampType & RampType.TopBackLeft) == RampType.TopBackLeft)
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);
            }

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
                    var v = new TemporaryVoxelHandle(Chunk, new LocalVoxelCoordinate(x, Y, z));
                    bool isTop = false;

                    if (Y < VoxelConstants.ChunkSizeY - 1)
                    {
                        var vAbove = new TemporaryVoxelHandle(Chunk, new LocalVoxelCoordinate(x, Y + 1, z));

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
                                var handle = new TemporaryVoxelHandle(Chunk.Manager.ChunkData, n);
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

        private static VertexColorInfo CalculateVertexLight(TemporaryVoxelHandle Vox, VoxelVertex Vertex,
            ChunkManager chunks)
        {
            int neighborsEmpty = 0;
            int neighborsChecked = 0;

            var color = new VertexColorInfo();
            color.DynamicColor = 0;
            color.SunColor = 0;

            foreach (var c in VoxelHelpers.EnumerateVertexNeighbors(Vox.Coordinate, Vertex))
            {
                var v = new TemporaryVoxelHandle(chunks.ChunkData, c);
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

        //private static void GetCacheID(TemporaryVoxelHandle Voxel, VoxelVertex Vertex)
        //{
        //    var coordinate = Voxel.Coordinate;
        //    switch (Vertex)
        //    {
        //        case VoxelVertex.
        //    }
        //}

        private static BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(TemporaryVoxelHandle V)
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
            TemporaryVoxelHandle V,
            VoxelType Type)
        {
            if (Type.Transitions == VoxelType.TransitionType.Horizontal)
            {
                var value = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate)
                    .Select(c => new TemporaryVoxelHandle(Data, c)), Type);

                return new BoxTransition()
                {
                    Top = (TransitionTexture)value
                };
            }
            else
            {
                var transitionFrontBack = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.Z)
                    .Select(c => new TemporaryVoxelHandle(Data, c)),
                    Type);

                var transitionLeftRight = ComputeTransitionValueOnPlane(
                    VoxelHelpers.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.X)
                    .Select(c => new TemporaryVoxelHandle(Data, c)),
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

        private static int ComputeTransitionValueOnPlane(IEnumerable<TemporaryVoxelHandle> Neighbors, VoxelType Type)
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
    }

}