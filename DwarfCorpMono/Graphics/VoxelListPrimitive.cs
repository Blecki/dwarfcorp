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
    public class VoxelListPrimitive : GeometricPrimitive
    {

        public static ConcurrentDictionary<BoxFace, Vector3> faceDeltas = new ConcurrentDictionary<BoxFace, Vector3>();
        Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
        Dictionary<BoxFace, bool> drawFace = new Dictionary<BoxFace, bool>();
        List<ExtendedVertex> accumulatedVertices = new List<ExtendedVertex>();
        bool isRebuilding = false;
        Mutex rebuildMutex = new Mutex();
        public static bool staticInitialized = false;

        public void InitializeStatics()
        {
            if (!staticInitialized)
            {
                faceDeltas[BoxFace.Back] = new Vector3(0, 0, 1);
                faceDeltas[BoxFace.Front] = new Vector3(0, 0, -1);
                faceDeltas[BoxFace.Left] = new Vector3(-1, 0, 0);
                faceDeltas[BoxFace.Right] = new Vector3(1, 0, 0);
                faceDeltas[BoxFace.Top] = new Vector3(0, 1, 0);
                faceDeltas[BoxFace.Bottom] = new Vector3(0, -1, 0);
                CreateFaceDrawMap();
                staticInitialized = true;
            }
        }


        public VoxelListPrimitive() :
            base()
        {
            InitializeStatics();
            
        }


        class IntVector
        {

            public int x;
            public int y;
            public int z;

            public IntVector(Vector3 Vec)
            {
                x = (int)Vec.X;
                y = (int)Vec.Y;
                z = (int)Vec.Z;
            }

            public override bool Equals(object obj)
            {
                if (obj is IntVector)
                {
                    return (x == ((IntVector)obj).x && y == ((IntVector)obj).y && z == ((IntVector)obj).z);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return x ^ y ^ z;
            }
        }

        public static bool IsTopVertex(VoxelVertex v)
        {
            return v == VoxelVertex.BackTopLeft || v == VoxelVertex.FrontTopLeft || v == VoxelVertex.FrontTopRight || v == VoxelVertex.BackTopRight ;
        }

        public static bool ShouldRamp(VoxelVertex vertex, RampType rampType)
        {
            bool toReturn = false;

            if(Voxel.HasFlag(rampType, RampType.TopFrontRight))
            {
                toReturn = toReturn || (vertex == VoxelVertex.BackTopRight);
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
                || (Voxel.HasFlag(rampType, RampType.Left) && Voxel.HasFlag(rampType, RampType.Right)) 
                || (Voxel.HasFlag(rampType, RampType.Front) && Voxel.HasFlag(rampType, RampType.Back));
        }

        public static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }

       

        public static void UpdateCornerRamps(VoxelChunk chunk)
        {
            Voxel v = null;
            List<Voxel> neighbors = new List<Voxel>();
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int y = 0; y < chunk.SizeY; y++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                    
                        v = chunk.VoxelGrid[x][y][z];
                        bool isTop = false;


                        if (y < chunk.SizeY - 1)
                        {
                            Voxel vAbove = chunk.VoxelGrid[x][y + 1][z];

                            if (vAbove != null)
                            {
                                isTop = false;
                            }
                            else
                            {
                                isTop = true;
                            }
                        }

                        List<VoxelRef> diagNeighbors = new List<VoxelRef>();
                        ExtendedVertex[] faceVertices;
                        if (v != null && v.IsVisible && isTop  && v.Type.canRamp)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                if (face != BoxFace.Bottom)
                                {

                                    faceVertices = v.Primitive.GetFace(face);
                                    foreach (ExtendedVertex vertex in faceVertices)
                                    {
                                        bool emptyFound = false;
                                        VoxelVertex bestKey = VoxelChunk.GetNearestDelta(vertex.Position);
                                        chunk.GetNeighborsVertexDiag(bestKey, x, y, z, diagNeighbors, true);

                                        foreach (VoxelRef vox in diagNeighbors)
                                        {
                                            if (vox.TypeName == "empty")
                                            {
                                                emptyFound = true;
                                                break;
                                            }
                                        }

                                        if (emptyFound)
                                        {
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

                     }

                    
                }
            }
        }

        static bool[, ,] faceDrawMap = new bool[6, (int)RampType.All + 1, (int)RampType.All + 1];

        static void CreateFaceDrawMap()
        {
            #region BULLSHIT
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Back, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Front, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Left, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.All] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = true;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Right, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Top, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.None, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.All, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Front, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Left, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Right, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.Back, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontLeft, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopFrontRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackLeft, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)RampType.TopBackRight, (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Front | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Left), (int)(RampType.Back | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.None] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.All] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.Front] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.Left] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.Right] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.Back] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.TopFrontRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackLeft] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)RampType.TopBackRight] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)(RampType.Front | RampType.Right)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Left)] = false;
            faceDrawMap[(int)BoxFace.Bottom, (int)(RampType.Back | RampType.Right), (int)(RampType.Back | RampType.Right)] = false;

            #endregion
        }

        public static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {

           
            if (face == BoxFace.Top || face == BoxFace.Bottom)
            {
                return true;
            }



            return faceDrawMap[(int)face, (int)myRamp, (int)neighborRamp] ;
        }
        
        public static void UpdateRamps(VoxelChunk chunk)
        {
            Dictionary<BoxFace, bool> faceExists = new Dictionary<BoxFace, bool>();
            Dictionary<BoxFace, bool> faceVisible = new Dictionary<BoxFace, bool>();
            Voxel v;
            Vector3 delta;
            List<Voxel> neighbors = new List<Voxel>();
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int y = 0; y < Math.Min(chunk.Manager.MaxViewingLevel + 1, chunk.SizeY); y++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        v = chunk.VoxelGrid[x][y][z];
                        bool isTop = false;


                        if (y < chunk.SizeY - 1)
                        {
                            Voxel vAbove = chunk.VoxelGrid[x][y + 1][z];

                            if (vAbove != null)
                            {
                                isTop = false;
                            }
                            else
                            {
                                isTop = true;
                            }
                        }


                        if (isTop && v != null && v.IsVisible && v.Type.canRamp)
                        {

                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                if (!IsSideFace(face))
                                {
                                    continue;
                                }

                                delta = faceDeltas[face];
                                faceExists[face] = chunk.IsCellValid(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z);
                                faceVisible[face] = true;

                                if (faceExists[face])
                                {
                                    Voxel voxelOnFace = chunk.VoxelGrid[x + (int)delta.X][y + (int)delta.Y][z + (int)delta.Z];

                                    if (voxelOnFace == null || !voxelOnFace.IsVisible)
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
                                    neighbors.Clear();
                                    chunk.Manager.GetNonNullVoxelsAtWorldLocationCheckFirst(null, new Vector3(x + (int)delta.X + 0.5f, y + (int)delta.Y + 0.5f, z + (int)delta.Z + 0.5f) + chunk.Origin, neighbors, 0);

                                    if (neighbors.Count > 0)
                                    {
                                        bool isAnyFaceEmpty = false;
                                        foreach (Voxel n in neighbors)
                                        {
                                            if (n == null || !n.IsVisible)
                                            {
                                                isAnyFaceEmpty = true;
                                                break;
                                            }
                                        }

                                        if (isAnyFaceEmpty)
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
                                        faceVisible[face] = false;
                                    }

                                }


                                if (isTop
                                    && faceVisible[face])
                                {
                                    v.RampType = v.RampType | UpdateRampType(face);
                                }


                            }

                            if (RampIsDegenerate(v.RampType))
                            {
                                v.RampType = RampType.None;
                            }


                        }
                        else if (v != null && v.IsVisible && v.Type.canRamp)
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
            if (isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();


            accumulatedVertices.Clear();
            faceExists.Clear();
            drawFace.Clear();

            Vector3 delta = Vector3.Zero;
            Voxel v = null;
            ExtendedVertex[] faceVertices;
            List<Voxel> neighbors = new List<Voxel>();
            List<VoxelRef> neighborRef = new List<VoxelRef>();
            VoxelChunk.VertexColorInfo colorInfo = new VoxelChunk.VertexColorInfo();

            Dictionary<IntVector, List<KeyValuePair<VertexPositionColorTexture, int>>> indexedVertices = new Dictionary<IntVector, List<KeyValuePair<VertexPositionColorTexture, int>>>();
            BoxPrimitive primitive = null;
            for (int x = 0; x < chunk.SizeX; x++)
            {
                for (int y = 0; y < Math.Min(chunk.Manager.MaxViewingLevel + 1, chunk.SizeY); y++)
                {
                    for (int z = 0; z < chunk.SizeZ; z++)
                    {
                        v = chunk.VoxelGrid[x][y][ z];


                        if (v != null && v.IsVisible)
                        {

                            if (!v.Type.specialRampTextures)
                            {
                                primitive = v.Primitive;
                            }
                            else
                            {
                                if (v.Type.RampPrimitives.ContainsKey(v.RampType))
                                {
                                    primitive = v.Type.RampPrimitives[v.RampType];
                                }
                                else
                                {
                                    primitive = v.Primitive;
                                }
                            }

                            float texScale = (float)primitive.UVs.m_cellHeight / (float)primitive.UVs.m_texHeight;


                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                delta = faceDeltas[face];
                                faceExists[face] = chunk.IsCellValid(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z);
                                drawFace[face] = true;

                                if (faceExists[face])
                                {
                                    Voxel voxelOnFace = chunk.VoxelGrid[x + (int)delta.X][ y + (int)delta.Y][ z + (int)delta.Z];

                                    if (voxelOnFace == null || !voxelOnFace.IsVisible || ((voxelOnFace.Type.canRamp && voxelOnFace.RampType != RampType.None && IsSideFace(face) && ShouldDrawFace(face, voxelOnFace.RampType, v.RampType))))
                                    {
                                        drawFace[face] = true;
                                    }
                                    else
                                    {
                                        drawFace[face] = false;
                                    }
                                }
                                else
                                {
                                    neighbors.Clear();
                                    chunk.Manager.GetNonNullVoxelsAtWorldLocationCheckFirst(null, new Vector3(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z) + chunk.Origin, neighbors, 0);

                                    if (neighbors.Count > 0)
                                    {
                                        bool isAnyFaceEmpty = false;
                                        foreach (Voxel n in neighbors)
                                        {
                                            if (n == null || !n.IsVisible || ((n.Type.canRamp && n.RampType != RampType.None && IsSideFace(face) && ShouldDrawFace(face, n.RampType, v.RampType))))
                                            {
                                                isAnyFaceEmpty = true;
                                                break;
                                            }
                                        }

                                        if (isAnyFaceEmpty)
                                        {
                                            drawFace[face] = true;
                                        }
                                        else
                                        {
                                            drawFace[face] = false;
                                        }
                                    }
                                    else if (!v.Chunk.IsCompletelySurrounded(v.GetReference(), true))
                                    {
                                        drawFace[face] = true;
                                    }
                                    else
                                    {
                                        drawFace[face] = false;
                                    }
                                }

                            }


                            for (int i = 0; i < 6; i++)
                            {
                                BoxFace face = (BoxFace)i;
                                if (drawFace[face])
                                {

                                    faceVertices = primitive.GetFace(face);
                                    foreach (ExtendedVertex vertex in faceVertices)
                                    {
                                        colorInfo.AmbientColor = 0;
                                        colorInfo.DynamicColor = 0;
                                        colorInfo.SunColor = 0;
                                        VoxelVertex bestKey = VoxelChunk.GetNearestDelta(vertex.Position);
                                        //VoxelChunk.CalculateVertexLight(v, bestKey, chunk.Manager, neighborRef, ref colorInfo);
                                        

                                        //Color color = new Color(colorInfo.SunColor,colorInfo.AmbientColor, colorInfo.DynamicColor);
                                        Color color = v.VertexColors[(int)bestKey];
                                        Vector3 offset = Vector3.Zero;
                                        Vector2 texOffset = Vector2.Zero;

                                        if (v.Type.canRamp && ShouldRamp(bestKey, v.RampType))
                                        {
                                            offset = new Vector3(0, -v.Type.rampSize, 0);

                                            if (face != BoxFace.Top && face != BoxFace.Bottom)
                                            {
                                                texOffset = new Vector2(0, v.Type.rampSize * (texScale));
                                            }
                                        }



                                        ExtendedVertex newVertex = new ExtendedVertex((vertex.Position + v.Position + VertexNoise.GetNoiseVectorFromRepeatingTexture(vertex.Position + v.Position) + offset),
                                                                                                                                                        color,
                                                                               vertex.TextureCoordinate + texOffset, vertex.TextureBounds);
                                        accumulatedVertices.Add(newVertex);
                                    }
                                }
                            }

                        }
                      
                    }
                }
            }


            m_vertices = new ExtendedVertex[accumulatedVertices.Count];

            for (int i = 0; i < accumulatedVertices.Count; i++)
            {
                m_vertices[i] = accumulatedVertices[i];
            }

            ResetBuffer(graphics);
            isRebuilding = false;

            chunk.PrimitiveMutex.WaitOne();
            chunk.NewPrimitive = this;
            chunk.NewPrimitiveReceived = true;
            chunk.PrimitiveMutex.ReleaseMutex();
            
        }



        public bool ContainsNearVertex(Vector3 vertexPos1, List<VertexPositionColorTexture> accumulated)
        {
            float epsilon = 0.001f;

            foreach (VertexPositionColorTexture vert in accumulated)
            {
                float dist = (vertexPos1 - (vert.Position)).LengthSquared();

                if (dist < epsilon)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
