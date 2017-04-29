using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a collection of voxels with a surface mesh. Efficiently culls away
    /// invisible voxels, and properly constructs ramps.
    /// </summary>
    public class VoxelListPrimitive : GeometricPrimitive, IDisposable
    {
        public static readonly Vector3[] FaceDeltas = new Vector3[6];
        private static readonly List<Vector3>[] VertexNeighbors2D = new List<Vector3>[8];
        private readonly bool[] faceExists = new bool[6];
        private readonly bool[] drawFace = new bool[6];
        private bool isRebuilding;
        private readonly Mutex rebuildMutex = new Mutex();
        private static bool StaticInitialized;

        private void InitializeStatics()
        {
            if (!StaticInitialized)
            {
                FaceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);
                VertexNeighbors2D[(int)VoxelVertex.FrontTopLeft] = new List<Vector3>()
                {
                    new Vector3(-1, 0, 0),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, 0, 1)
                };
                VertexNeighbors2D[(int)VoxelVertex.FrontTopRight] = new List<Vector3>()
                {
                    new Vector3(0, 0, 1),
                    new Vector3(1, 0, 1),
                    new Vector3(1, 0, 0)
                };
                VertexNeighbors2D[(int)VoxelVertex.BackTopLeft] = new List<Vector3>()
                {
                    new Vector3(-1, 0, 0),
                    new Vector3(-1, 0, -1),
                    new Vector3(0, 0, -1)
                };
                VertexNeighbors2D[(int)VoxelVertex.BackTopRight] = new List<Vector3>()
                {
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(1, 0, 0)
                };
                CreateFaceDrawMap();
                StaticInitialized = true;
            }
        }

        public VoxelListPrimitive() 
        {
            InitializeStatics();
        }

        private static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if (Voxel.HasFlag(rampType, RampType.TopFrontRight))
            {
                toReturn = (vertex == VoxelVertex.BackTopRight);
            }

            if (Voxel.HasFlag(rampType, RampType.TopBackRight))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopRight);
            }

            if (Voxel.HasFlag(rampType, RampType.TopFrontLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);
            }

            if (Voxel.HasFlag(rampType, RampType.TopBackLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);
            }


            return toReturn;
        }

        public static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }

        public static void UpdateCornerRamps(VoxelChunk chunk)
        {
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vAbove = chunk.MakeVoxel(0, 0, 0);
            List<Voxel> diagNeighbors = chunk.AllocateVoxels(3);
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

        private static readonly bool[,,] FaceDrawMap = new bool[6, (int)RampType.All + 1, (int)RampType.All + 1];

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

        private static void CreateFaceDrawMap()
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
                        }

                        FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = (their1 < my1 || their2 < my2);
                    }
                }
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

        public void InitializeFromChunk(VoxelChunk chunk)
        {
            if (chunk == null)
            {
                return;
            }

            rebuildMutex.WaitOne();
            if (isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();
            int[] ambientValues = new int[4];
            int maxIndex = 0;
            int maxVertex = 0;
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            Voxel[] manhattanNeighbors = new Voxel[4];
            BoxPrimitive bedrockModel = VoxelLibrary.GetPrimitive("Bedrock");
            Voxel worldVoxel = new Voxel();

            if (Vertices == null)
            {
                Vertices = new ExtendedVertex[1024];
            }

            if (Indexes == null)
            {
                Indexes = new ushort[512];
            }

            for (int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
            {
                for (int x = 0; x < chunk.SizeX; x++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.GetVoxel_A");
                        v.GridPosition = new Vector3(x, y, z);
                        //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.GetVoxel_A");

                        if ((v.IsExplored && v.IsEmpty) || !v.IsVisible)
                        {
                            continue;
                        }

                        //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.AllButEmpty_A");
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
                            uvs = v.ComputeTransitionTexture(manhattanNeighbors);
                        }

                        //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.CalculateFaces_A");
                        for (int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace)i;
                            Vector3 delta = FaceDeltas[(int)face];
                            faceExists[(int)face] = chunk.IsCellValid(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z);
                            drawFace[(int)face] = true;

                            if (faceExists[(int)face])
                            {
                                //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.GetVoxel_A");
                                voxelOnFace.GridPosition = new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z);
                                //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.GetVoxel_A");
                                drawFace[(int)face] = (voxelOnFace.IsExplored && voxelOnFace.IsEmpty) || !voxelOnFace.IsVisible ||
                                    (voxelOnFace.Type.CanRamp && voxelOnFace.RampType != RampType.None && IsSideFace(face) &&
                                    ShouldDrawFace(face, voxelOnFace.RampType, v.RampType));
                            }
                            else
                            {
                                //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.GetVoxel_A");
                                bool success = chunk.Manager.ChunkData.GetNonEmptyVoxelAtWorldLocation(new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z) + chunk.Origin, ref worldVoxel);
                                //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.GetVoxel_A");
                                if (success)
                                {
                                    drawFace[(int)face] = (worldVoxel.IsExplored && worldVoxel.IsEmpty) || !worldVoxel.IsVisible ||
                                                         (worldVoxel.Type.CanRamp && worldVoxel.RampType != RampType.None &&
                                                          IsSideFace(face) &&
                                                          ShouldDrawFace(face, worldVoxel.RampType, v.RampType));
                                }
                                else
                                {
                                    drawFace[(int)face] = false;
                                }
                            }
                        }
                        //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.CalculateFaces_A");

                        //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.PlaceVertices_A");
                        for (int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace)i;
                            if (!drawFace[(int)face])
                            {
                                continue;
                            }


                            int faceIndex;
                            int faceCount;
                            int vertexIndex;
                            int vertexCount;
                            primitive.GetFace(face, uvs, out faceIndex, out faceCount, out vertexIndex, out vertexCount);
                            Vector2 texScale = uvs.Scales[i];

                            int indexOffset = maxVertex;
                            for (int vertOffset = 0; vertOffset < vertexCount; vertOffset++)
                            {
                                ExtendedVertex vert = primitive.Vertices[vertOffset + vertexIndex];
                                VoxelVertex bestKey = primitive.Deltas[vertOffset + vertexIndex];
                                Color color = v.Chunk.Data.GetColor(x, y, z, bestKey);
                                ambientValues[vertOffset] = color.G;
                                Vector3 offset = Vector3.Zero;
                                Vector2 texOffset = Vector2.Zero;

                                if (v.Type.CanRamp && ShouldRamp(bestKey, v.RampType))
                                {
                                    offset = new Vector3(0, -v.Type.RampSize, 0);

                                    if (face != BoxFace.Top && face != BoxFace.Bottom)
                                    {
                                        texOffset = new Vector2(0, v.Type.RampSize * (texScale.Y));
                                    }
                                }

                                if (maxVertex >= Vertices.Length)
                                {
                                    ExtendedVertex[] newVertices = new ExtendedVertex[Vertices.Length * 2];
                                    Vertices.CopyTo(newVertices, 0);
                                    Vertices = newVertices;
                                }

                                Vertices[maxVertex] = new ExtendedVertex(vert.Position + v.Position +
                                                                   VertexNoise.GetNoiseVectorFromRepeatingTexture(
                                                                       vert.Position + v.Position) + offset,
                                    color,
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
                                ushort vertexOffset0 = flippedQuad ? primitive.FlippedIndexes[faceIndex] : primitive.Indexes[faceIndex];
                                Indexes[maxIndex] =
                                    (ushort)(indexOffset + (vertexOffset - vertexOffset0));
                                maxIndex++;
                            }
                        }
                        //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.AllButEmpty_A");
                        //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.PlaceVertices_A");
                    }
                }
            }
            MaxIndex = maxIndex;
            MaxVertex = maxVertex;
            GamePerformance.Instance.TrackReferenceType("Lightmap reference", (Lightmap == null) ? "False" : "True");
            GenerateLightmap(chunk.Manager.ChunkData.Tilemap.Bounds);
            lock(VertexLock)
            {
                ResetVertexBuffer = true;
            }
            isRebuilding = false;
        }

        public int InitializeFromChunkNew(VoxelChunk chunk, int highestVoxel)
        {
            if (chunk == null) return highestVoxel;

            rebuildMutex.WaitOne();
            if (isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return highestVoxel;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();
            int[] ambientValues = new int[4];
            int maxIndex = 0;
            int maxVertex = 0;
            Voxel[] manhattanNeighbors = new Voxel[4];
            BoxPrimitive bedrockModel = VoxelLibrary.GetPrimitive("Bedrock");

            if (Vertices == null)
            {
                Vertices = new ExtendedVertex[1024];
            }

            if (Indexes == null)
            {
                Indexes = new ushort[512];
            }

            int maxFloor = Math.Min((int)chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY);

            int newMaxFloor = Math.Min(maxFloor, highestVoxel + 1);

            // We'll use this to track the highest floor we found a non-empty voxel on for the return value.
            int highestFloor = -1;
            int voxelCount = 0;
            int voxelFaceDrawCount = 0;

            for (int y = 0; y < newMaxFloor; y++)
            {
                bool topFloor = (y == (newMaxFloor - 1));
                bool voxelFound = false;
                for (int x = 0; x < chunk.SizeX; x++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        voxelCount++;
                        //if (chunk.ID.Is(37, 0, 17) && (x == 10 && y == 23 && z == 12))
                        //    System.Diagnostics.Debugger.Break();
                        int index = chunk.Data.IndexAt(x, y, z);

                        VoxelFlagHelper flags = chunk.Data.GetFlagHelper(index);
                        if (!flags.GetFlag(VoxelFlags.IsEmpty)) voxelFound = true;

                        // If we don't have a voxel that has faces to render
                        if (!flags.GetFlag(VoxelFlags.FacesToRender)) continue;

                        // We can put a check here where if we aren't on the top floor and all face flags are empty
                        // we can just skip that thing instead of checking all six faces.  If we are on the top floor
                        // any voxel with FacesToRender being true must draw it's top face at least.
                        if (!topFloor)
                        {
                            // Otherwise check the actual flag.
                            if (!flags.GetAnyFlag(VoxelFlags.AllFaces)) continue;
                        }


                        BoxPrimitive primitive;
                        if (!flags.GetFlag(VoxelFlags.IsExplored))
                        {
                            primitive = bedrockModel;
                        }
                        else
                        {
                            primitive = chunk.Data.GetPrimitiveByTypeIndex(index);
                        }

                        VoxelType vType = chunk.Data.GetVoxelType(index);

                        Color tint = vType.Tint;
                        BoxPrimitive.BoxTextureCoords uvs;

                        // Instead of getting IsExplored again we're going to compare the primitive to the bedrock.
                        if (vType.HasTransitionTextures && primitive != bedrockModel)
                        {
                            uvs = vType.TransitionTextures[chunk.ComputeTransitionValue(x, y, z, manhattanNeighbors)];
                        }
                        else
                        {
                            uvs = primitive.UVs;
                        }

                        Vector3 vPosition = new Vector3(x, y, z) + chunk.Origin;

                        voxelFaceDrawCount++;
                        //GamePerformance.Instance.StartTrackPerformance("VC.Rebuild.PlaceVertices_B");
                        for (int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace)i;

                            // If we've made it this far it means we have a valid voxel.
                            // If we are on the top face and are on the top floor we have a face to draw.
                            if (face != BoxFace.Top || !topFloor)
                            {
                                // Otherwise check the actual flag.
                                if (!flags.GetFlag(VoxelChunk.BoxFaceToVoxelFlag(face))) continue;
                            }

                            int faceIndex;
                            int faceCount;
                            int vertexIndex;
                            int vertexCount;
                            primitive.GetFace(face, uvs, out faceIndex, out faceCount, out vertexIndex, out vertexCount);
                            Vector2 texScale = uvs.Scales[i];

                            int indexOffset = maxVertex;
                            for (int vertOffset = 0; vertOffset < vertexCount; vertOffset++)
                            {
                                ExtendedVertex vert = primitive.Vertices[vertOffset + vertexIndex];
                                VoxelVertex bestKey = primitive.Deltas[vertOffset + vertexIndex];
                                Color color = chunk.Data.GetColor(x, y, z, bestKey);
                                ambientValues[vertOffset] = color.G;
                                Vector3 offset = Vector3.Zero;
                                Vector2 texOffset = Vector2.Zero;

                                if (vType.CanRamp && ShouldRamp(bestKey, chunk.Data.RampTypes[index]))
                                {
                                    offset = new Vector3(0, -vType.RampSize, 0);

                                    if (face != BoxFace.Top && face != BoxFace.Bottom)
                                    {
                                        texOffset = new Vector2(0, vType.RampSize * (texScale.Y));
                                    }
                                }

                                if (maxVertex >= Vertices.Length)
                                {
                                    ExtendedVertex[] newVertices = new ExtendedVertex[Vertices.Length * 2];
                                    Vertices.CopyTo(newVertices, 0);
                                    Vertices = newVertices;
                                }

                                Vertices[maxVertex] = new ExtendedVertex(vert.Position + vPosition +
                                                                   VertexNoise.GetNoiseVectorFromRepeatingTexture(
                                                                       vert.Position + vPosition) + offset,
                                    color,
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
                                ushort vertexOffset0 = flippedQuad ? primitive.FlippedIndexes[faceIndex] : primitive.Indexes[faceIndex];
                                Indexes[maxIndex] =
                                    (ushort)(indexOffset + (vertexOffset - vertexOffset0));
                                maxIndex++;
                            }
                        }
                        //GamePerformance.Instance.StopTrackPerformance("VC.Rebuild.PlaceVertices_B");
                    }
                }
                if (voxelFound) highestFloor = y;
            }

            GamePerformance.Instance.TrackReferenceType("VoxelCounts", voxelCount + "/" + voxelFaceDrawCount);
            MaxIndex = maxIndex;
            MaxVertex = maxVertex;
            GamePerformance.Instance.StartTrackPerformance("VertexLock");
            GamePerformance.Instance.StopTrackPerformance("VertexLock");

            GamePerformance.Instance.TrackReferenceType("Lightmap reference", (Lightmap == null) ? "False" : "True");
            GamePerformance.Instance.StartTrackPerformance("LightMap");
            GenerateLightmap(chunk.Manager.ChunkData.Tilemap.Bounds);
            GamePerformance.Instance.StopTrackPerformance("LightMap");
            lock (VertexLock)
            {
                ResetVertexBuffer = true;
            }
            isRebuilding = false;

            // At the end here we are going to return the highest floor we found during the scan.
            // However this is going to cause issues if we didn't scan all the way to the floor where
            // the highest passed in was found.
            // So if our highest floor passed is higher than the top floor we scanned to we will
            // return the value passed in.  Once unsliced we will be able to scan properly again.

            if (highestFloor < maxFloor - 1) return highestFloor;

            if (highestVoxel >= maxFloor) return highestVoxel;
            else return highestFloor;
        }

        public void Dispose()
        {
            rebuildMutex.Dispose();
        }
    }

}