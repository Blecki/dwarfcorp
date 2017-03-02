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
        public static Vector3[] FaceDeltas = new Vector3[6];
        public static List<Vector3>[] VertexNeighbors2D = new List<Vector3>[8];
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


        public VoxelListPrimitive() :
            base()
        {
            InitializeStatics();
        }


        public static bool IsTopVertex(VoxelVertex v)
        {
            return v == VoxelVertex.BackTopLeft || v == VoxelVertex.FrontTopLeft || v == VoxelVertex.FrontTopRight || v == VoxelVertex.BackTopRight;
        }

        public static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if(Voxel.HasFlag(rampType, RampType.TopFrontRight))
            {
                toReturn = (vertex == VoxelVertex.BackTopRight);
            }

            if(Voxel.HasFlag(rampType, RampType.TopBackRight))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopRight);
            }

            if(Voxel.HasFlag(rampType, RampType.TopFrontLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopLeft);
            }

            if(Voxel.HasFlag(rampType, RampType.TopBackLeft))
            {
                toReturn = toReturn || (vertex == VoxelVertex.FrontTopLeft);
            }


            return toReturn;
        }

        public static RampType UpdateRampType(BoxFace face)
        {
            switch(face)
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
                   || (Voxel.HasFlag(rampType, RampType.Left) && Voxel.HasFlag(rampType, RampType.Right))
                   || (Voxel.HasFlag(rampType, RampType.Front) && Voxel.HasFlag(rampType, RampType.Back));
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

            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < chunk.SizeY; y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z);
                        bool isTop = false;


                        if(y < chunk.SizeY - 1)
                        {
                            vAbove.GridPosition =  new Vector3(x, y + 1, z); 

                            isTop = vAbove.IsEmpty;
                        }

                        if(v.IsEmpty || !v.IsVisible || !isTop || !v.Type.CanRamp)
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

                            if(!emptyFound)
                            {
                                continue;
                            }

                            switch(bestKey)
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

        private static readonly bool[,,] FaceDrawMap = new bool[6, (int) RampType.All + 1, (int) RampType.All + 1];

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
            BoxFace[] faces = (BoxFace[]) Enum.GetValues(typeof (BoxFace));

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
                            FaceDrawMap[(int) neighborFace, (int) myRamp, (int) neighborRamp] = false;
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

                        FaceDrawMap[(int)neighborFace, (int)myRamp, (int)neighborRamp] = (their1 < my1 || their2 < my2 );
                    }
                }
            }

   
        }

        public static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            if(face == BoxFace.Top || face == BoxFace.Bottom)
            {
                return true;
            }


            return FaceDrawMap[(int) face, (int) myRamp, (int) neighborRamp];
        }

        public static void UpdateRamps(VoxelChunk chunk)
        {
            Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
            Dictionary<BoxFace, bool> faceVisible = new Dictionary<BoxFace, bool>();
            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel vAbove = chunk.MakeVoxel(0, 0, 0);
            Voxel voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            Voxel worldVoxel = new Voxel();

            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z);
                        bool isTop = false;


                        if(y < chunk.SizeY - 1)
                        {
                            vAbove.GridPosition = new Vector3(x, y + 1, z);

                            isTop = vAbove.IsEmpty;
                        }


                        if(isTop && !v.IsEmpty && v.IsVisible && v.Type.CanRamp)
                        {

                            for(int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace) i;
                                if(!IsSideFace(face))
                                {
                                    continue;
                                }

                                Vector3 delta = FaceDeltas[(int)face];
                                faceExists[face] = chunk.IsCellValid(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                                faceVisible[face] = true;

                                if(faceExists[face])
                                {
                                    voxelOnFace.GridPosition = new Vector3(x + (int) delta.X, y + (int) delta.Y,
                                        z + (int) delta.Z);

                                    if(voxelOnFace.IsEmpty || !voxelOnFace.IsVisible)
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
                                    if (!chunk.Manager.ChunkData.GetNonEmptyVoxelAtWorldLocation(new Vector3(x + (int)delta.X + 0.5f, y + (int)delta.Y + 0.5f, z + (int)delta.Z + 0.5f) + chunk.Origin, ref worldVoxel) || !worldVoxel.IsVisible)
                                    {
                                        faceVisible[face] = true;
                                    }
                                    else
                                    {
                                        faceVisible[face] = false;
                                    }
                                }


                                if(faceVisible[face])
                                {
                                    v.RampType = v.RampType | UpdateRampType(face);
                                }
                            }

                            if(RampIsDegenerate(v.RampType))
                            {
                                v.RampType = RampType.None;
                            }
                        }
                        else if(!v.IsEmpty && v.IsVisible && v.Type.CanRamp)
                        {
                            v.RampType = RampType.None;
                        }
                    }
                }
            }
        }

        public void InitializeFromChunk(VoxelChunk chunk, GraphicsDevice graphics)
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
                for(int x = 0; x < chunk.SizeX; x++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z); 


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
                            uvs = v.ComputeTransitionTexture(manhattanNeighbors);
                        }

                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            Vector3 delta = FaceDeltas[(int)face];
                            faceExists[(int)face] = chunk.IsCellValid(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                            drawFace[(int)face] = true;

                            if(faceExists[(int)face])
                            {
                                voxelOnFace.GridPosition = new Vector3(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                                drawFace[(int)face] =  (voxelOnFace.IsExplored && voxelOnFace.IsEmpty) || !voxelOnFace.IsVisible || 
                                    (voxelOnFace.Type.CanRamp && voxelOnFace.RampType != RampType.None && IsSideFace(face) && 
                                    ShouldDrawFace(face, voxelOnFace.RampType, v.RampType));

                            }
                            else
                            {
                                bool success = chunk.Manager.ChunkData.GetNonEmptyVoxelAtWorldLocation(new Vector3(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z) + chunk.Origin, ref worldVoxel);
                                    drawFace[(int)face] = !success || (worldVoxel.IsExplored && worldVoxel.IsEmpty) || !worldVoxel.IsVisible ||
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

                                if(v.Type.CanRamp && ShouldRamp(bestKey, v.RampType))
                                {
                                    offset = new Vector3(0, -v.Type.RampSize, 0);

                                    if(face != BoxFace.Top && face != BoxFace.Bottom)
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