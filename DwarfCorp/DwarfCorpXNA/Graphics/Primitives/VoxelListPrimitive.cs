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
        private readonly bool[] faceExists = new bool[6];
        private readonly bool[] drawFace = new bool[6];
        private bool isRebuilding = false;
        private readonly Mutex rebuildMutex = new Mutex();
        public static bool StaticInitialized = false;

        public void InitializeStatics()
        {
            if(!StaticInitialized)
            {
                FaceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);
                
                CreateFaceDrawMap();
                StaticInitialized = true;
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
            {
                return;
            }

            rebuildMutex.WaitOne();
            if(isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();
            int[] ambientValues = new int[4];
            int maxIndex = 0;
            int maxVertex = 0;
            VoxelHandle v = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle[] manhattanNeighbors = new VoxelHandle[4];
            BoxPrimitive bedrockModel = VoxelLibrary.GetPrimitive("Bedrock");
            VoxelHandle worldVoxel = new VoxelHandle();
            List<VoxelHandle> lightingScratchSpace = new List<VoxelHandle>(8);
            if (Vertices == null)
            {
                Vertices = new ExtendedVertex[1024];
            }

            if (Indexes == null)
            {
                Indexes = new ushort[512];
            }

            for (int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, VoxelConstants.ChunkSizeY); y++)
            {
                for(int x = 0; x < VoxelConstants.ChunkSizeX; x++)
                {
                    for(int z = 0; z < VoxelConstants.ChunkSizeZ; z++)
                    {
                        v.GridPosition = new LocalVoxelCoordinate(x, y, z); 

                        if((v.IsExplored && v.IsEmpty) || !v.IsVisible)
                        {
                            continue;
                        }

                        BoxPrimitive primitive = VoxelLibrary.GetPrimitive(v.Type);
                        if (v.IsExplored && primitive == null) continue;
                        if (!v.IsExplored)
                        {
                            primitive = bedrockModel;
                        }

                        Color tint = v.Type.Tint;
                        BoxPrimitive.BoxTextureCoords uvs = primitive.UVs;

                        if (v.Type.HasTransitionTextures && v.IsExplored)
                        {
                            uvs = ComputeTransitionTexture(new TemporaryVoxelHandle(v.Chunk.Manager.ChunkData, v.Coordinate));
                        }

                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            Vector3 delta = FaceDeltas[(int)face];
                            faceExists[(int)face] = chunk.IsCellValid(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                            drawFace[(int)face] = true;

                            if(faceExists[(int)face])
                            {
                                voxelOnFace.GridPosition = new LocalVoxelCoordinate(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                                drawFace[(int)face] =  (voxelOnFace.IsExplored && voxelOnFace.IsEmpty) || (voxelOnFace.Type.IsTransparent && !v.Type.IsTransparent) || 
                                    !voxelOnFace.IsVisible || 
                                    (voxelOnFace.Type.CanRamp && voxelOnFace.RampType != RampType.None && IsSideFace(face) && 
                                    ShouldDrawFace(face, voxelOnFace.RampType, v.RampType));
                            }
                            else
                            {
                                bool success = chunk.Manager.ChunkData.GetNonNullVoxelAtWorldLocation(new Vector3(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z) + chunk.Origin, ref worldVoxel);
                                    drawFace[(int)face] = !success || (worldVoxel.IsExplored && worldVoxel.IsEmpty) || (worldVoxel.Type.IsTransparent && !v.Type.IsTransparent) || !worldVoxel.IsVisible ||
                                                     (worldVoxel.Type.CanRamp && worldVoxel.RampType != RampType.None &&
                                                      IsSideFace(face) &&
                                                      ShouldDrawFace(face, worldVoxel.RampType, v.RampType));
                            }
                        }


                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            if(!drawFace[(int)face])
                            {
                                continue;
                            }

                          
                            int faceIndex = 0;
                            int faceCount = 0;
                            int vertexIndex = 0;
                            int vertexCount = 0;
                            primitive.GetFace(face, uvs, out faceIndex, out faceCount, out vertexIndex, out vertexCount);
                            int indexOffset = maxVertex;
                            for (int vertOffset = 0; vertOffset < vertexCount; vertOffset++)
                            {
                                ExtendedVertex vert = primitive.Vertices[vertOffset + vertexIndex];
                                VoxelVertex bestKey = primitive.Deltas[vertOffset + vertexIndex];
                                //Color color = v.Chunk.Data.GetColor(x, y, z, bestKey);
                                Color color;
                                VoxelChunk.VertexColorInfo colorInfo = new VoxelChunk.VertexColorInfo();
                                VoxelChunk.CalculateVertexLight(v, bestKey, chunk.Manager, lightingScratchSpace, ref colorInfo);
                                ambientValues[vertOffset] = colorInfo.AmbientColor;
                                Vector3 offset = Vector3.Zero;
                                Vector2 texOffset = Vector2.Zero;

                                if(v.Type.CanRamp && ShouldRamp(bestKey, v.RampType))
                                {
                                    offset = new Vector3(0, -v.Type.RampSize, 0);
                                }

                                if (maxVertex >= Vertices.Length)
                                {
                                    ExtendedVertex[] newVertices = new ExtendedVertex[Vertices.Length * 2];
                                    Vertices.CopyTo(newVertices, 0);
                                    Vertices = newVertices;
                                }

                                Vertices[maxVertex] = new ExtendedVertex(vert.Position + v.WorldPosition +
                                                                   VertexNoise.GetNoiseVectorFromRepeatingTexture(
                                                                       vert.Position + v.WorldPosition) + offset,
                                    new Color(colorInfo.SunColor, colorInfo.AmbientColor, colorInfo.DynamicColor), 
                                    tint,
                                    uvs.Uvs[vertOffset + vertexIndex] + texOffset,
                                    uvs.Bounds[faceIndex / 6]);
                                maxVertex++;
                            }

                            bool flippedQuad = ambientValues[0] + ambientValues[2] > 
                                               ambientValues[1] + ambientValues[3];
                            for (int idx = faceIndex; idx < faceCount + faceIndex; idx++)
                            {
                                if (maxIndex >= Indexes.Length)
                                {
                                    ushort[] indexes = new ushort[Indexes.Length * 2];
                                    Indexes.CopyTo(indexes, 0);
                                    Indexes = indexes;
                                }

                                ushort vertexOffset = flippedQuad ? primitive.FlippedIndexes[idx] : primitive.Indexes[idx];
                                ushort vertexOffset0 = flippedQuad? primitive.FlippedIndexes[faceIndex] : primitive.Indexes[faceIndex];
                                Indexes[maxIndex] =
                                    (ushort) ((int)indexOffset + (int)((int)vertexOffset - (int)vertexOffset0));
                                maxIndex++;
                            }
                        }
                    }
                }
            }

            MaxIndex = maxIndex;
            MaxVertex = maxVertex;
            GenerateLightmap(chunk.Manager.ChunkData.Tilemap.Bounds);
            isRebuilding = false;

            //chunk.PrimitiveMutex.WaitOne();
            chunk.NewPrimitive = this;
            chunk.NewPrimitiveReceived = true;
            //chunk.PrimitiveMutex.ReleaseMutex();
        }

        private BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(TemporaryVoxelHandle V)
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
                var value = ComputeTransitionValueOnPlain(
                    DwarfCorp.Neighbors.EnumerateManhattanNeighbors2D(V.Coordinate)
                    .Select(c => new TemporaryVoxelHandle(Data, c)), Type);

                return new BoxTransition()
                {
                    Top = (TransitionTexture)value
                };
            }
            else
            {
                var transitionFrontBack = ComputeTransitionValueOnPlain(
                    DwarfCorp.Neighbors.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.Z)
                    .Select(c => new TemporaryVoxelHandle(Data, c)),
                    Type);

                var transitionLeftRight = ComputeTransitionValueOnPlain(
                    DwarfCorp.Neighbors.EnumerateManhattanNeighbors2D(V.Coordinate, ChunkManager.SliceMode.X)
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

        private static int ComputeTransitionValueOnPlain(IEnumerable<TemporaryVoxelHandle> Neighbors, VoxelType Type)
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

        public bool ContainsNearVertex(Vector3 vertexPos1, List<VertexPositionColorTexture> accumulated)
        {
            const float epsilon = 0.001f;

            return accumulated.Select(vert => (vertexPos1 - (vert.Position)).LengthSquared()).Any(dist => dist < epsilon);
        }

        public void Dispose()
        {
            rebuildMutex.Dispose();
        }
    }

}