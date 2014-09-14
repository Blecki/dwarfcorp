using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static ConcurrentDictionary<BoxFace, Vector3> FaceDeltas = new ConcurrentDictionary<BoxFace, Vector3>();
        private readonly Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
        private readonly Dictionary<BoxFace, bool> drawFace = new Dictionary<BoxFace, bool>();
        private readonly List<ExtendedVertex> accumulatedVertices = new List<ExtendedVertex>();
        private bool isRebuilding = false;
        private readonly Mutex rebuildMutex = new Mutex();
        public static bool StaticInitialized = false;

        public void InitializeStatics()
        {
            if(!StaticInitialized)
            {
                FaceDeltas[BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[BoxFace.Bottom] = new Vector3(0, -1, 0);
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
            List<Voxel> diagNeighbors = chunk.AllocateVoxels(VoxelChunk.VertexSuccessorsDiag[VoxelVertex.FrontTopLeft].Count);
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
                            continue;
                        }

                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            if(face == BoxFace.Bottom)
                            {
                                continue;
                            }

                            int faceIndex = 0;
                            int faceCount = 0;
                            v.Primitive.GetFace(face, v.Primitive.UVs, out faceIndex, out faceCount);

                            for (int idx = faceIndex; idx < faceIndex + faceCount; idx++)
                            {
                                VoxelVertex bestKey = VoxelChunk.GetNearestDelta(v.Primitive.Vertices[idx].Position);
                                chunk.GetNeighborsVertexDiag(bestKey, x, y, z, diagNeighbors);

                                bool emptyFound = diagNeighbors.Any(vox => vox.IsEmpty);

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
        }

        private static readonly bool[,,] FaceDrawMap = new bool[6, (int) RampType.All + 1, (int) RampType.All + 1];

        private static void CreateFaceDrawMap()
        {
            #region BULLSHIT

            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Back, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Front, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Left, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.All] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = true;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Right, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Top, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.None, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.All, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Front, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Left, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Right, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.Back, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontLeft, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopFrontRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackLeft, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) RampType.TopBackRight, (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Front | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Left), (int) (RampType.Back | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.None] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.All] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.Front] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.Left] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.Right] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.Back] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.TopFrontRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackLeft] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) RampType.TopBackRight] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) (RampType.Front | RampType.Right)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Left)] = false;
            FaceDrawMap[(int) BoxFace.Bottom, (int) (RampType.Back | RampType.Right), (int) (RampType.Back | RampType.Right)] = false;

            #endregion
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

                                Vector3 delta = FaceDeltas[face];
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
                                    Voxel worldVoxel = chunk.Manager.ChunkData.GetNonNullVoxelAtWorldLocation(new Vector3(x + (int)delta.X + 0.5f, y + (int)delta.Y + 0.5f, z + (int)delta.Z + 0.5f) + chunk.Origin);

                                    if (worldVoxel == null || !worldVoxel.IsVisible)
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
            rebuildMutex.WaitOne();
            if(isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();


            accumulatedVertices.Clear();
            faceExists.Clear();
            drawFace.Clear();

            Voxel v = chunk.MakeVoxel(0, 0, 0);
            Voxel voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            Voxel[] manhattanNeighbors = new Voxel[4];
            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z); 


                        if(v.IsEmpty || !v.IsVisible)
                        {
                            continue;
                        }

                        BoxPrimitive primitive = VoxelLibrary.GetPrimitive(v.Type);
                        BoxPrimitive.BoxTextureCoords uvs = primitive.UVs;

                        if(v.Type.HasTransitionTextures)
                        {
                            uvs = v.ComputeTransitionTexture(manhattanNeighbors);
                        }

                        float texScale = (float)uvs.m_cellHeight / (float)uvs.m_texHeight;


                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            Vector3 delta = FaceDeltas[face];
                            faceExists[face] = chunk.IsCellValid(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                            drawFace[face] = true;

                            if(faceExists[face])
                            {
                                voxelOnFace.GridPosition = new Vector3(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z);
                                drawFace[face] =  voxelOnFace.IsEmpty || !voxelOnFace.IsVisible || ((voxelOnFace.Type.CanRamp && voxelOnFace.RampType != RampType.None && IsSideFace(face) && ShouldDrawFace(face, voxelOnFace.RampType, v.RampType)));

                            }
                            else
                            {
                                Voxel worldVoxel = chunk.Manager.ChunkData.GetNonNullVoxelAtWorldLocation(new Vector3(x + (int) delta.X, y + (int) delta.Y, z + (int) delta.Z) + chunk.Origin);
                                drawFace[face] = worldVoxel == null || worldVoxel.IsEmpty || !worldVoxel.IsVisible || ((worldVoxel.Type.CanRamp && worldVoxel.RampType != RampType.None && IsSideFace(face) && ShouldDrawFace(face, worldVoxel.RampType, v.RampType)));
                            }
                        }


                        for(int i = 0; i < 6; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            if(!drawFace[face])
                            {
                                continue;
                            }
                            int faceIndex = 0;
                            int faceCount = 0;
                            primitive.GetFace(face, uvs, out faceIndex, out faceCount);
                            for(int idx = faceIndex; idx < faceIndex + faceCount; idx++)
                            {
                                
                                VoxelVertex bestKey = VoxelChunk.GetNearestDelta(primitive.Vertices[idx].Position);
                                Color color = v.Chunk.Data.GetColor(x, y, z, bestKey);
                                Vector3 offset = Vector3.Zero;
                                Vector2 texOffset = Vector2.Zero;

                                if(v.Type.CanRamp && ShouldRamp(bestKey, v.RampType))
                                {
                                    offset = new Vector3(0, -v.Type.RampSize, 0);

                                    if(face != BoxFace.Top && face != BoxFace.Bottom)
                                    {
                                        texOffset = new Vector2(0, v.Type.RampSize * (texScale));
                                    }
                                }


                                ExtendedVertex newVertex = new ExtendedVertex((primitive.Vertices[idx].Position + v.Position + VertexNoise.GetNoiseVectorFromRepeatingTexture(primitive.Vertices[idx].Position + v.Position) + offset),
                                    color,
                                    uvs.m_uvs[idx] + texOffset, uvs.Bounds[faceIndex / 6]);
                                accumulatedVertices.Add(newVertex);
                            }
                        }
                    }
                }
            }


            Vertices = new ExtendedVertex[accumulatedVertices.Count];
            accumulatedVertices.CopyTo(Vertices);

            ResetBuffer(graphics);
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
    }

}