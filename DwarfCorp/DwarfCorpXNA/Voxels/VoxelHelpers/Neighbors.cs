using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        #region Offset Lists
        private static GlobalVoxelOffset[] ManhattanNeighbors = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(0,1,0),
            new GlobalVoxelOffset(0,-1,0),
            new GlobalVoxelOffset(0,0,1),
            new GlobalVoxelOffset(0,0,-1)
        };

        private static GlobalVoxelOffset[] ManhattanCubeNeighbors = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(0,0,0),
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(0,1,0),
            new GlobalVoxelOffset(0,-1,0),
            new GlobalVoxelOffset(0,0,1),
            new GlobalVoxelOffset(0,0,-1)
        };

        public static GlobalVoxelOffset[] ManhattanNeighbors2D = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(0,0,-1),
            new GlobalVoxelOffset(0,0,1)
        };

        private static GlobalVoxelOffset[] AllNeighbors = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(-1,-1,-1),
            new GlobalVoxelOffset(-1,-1,0),
            new GlobalVoxelOffset(-1,-1,1),
            new GlobalVoxelOffset(-1,0,-1),
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(-1,0,1),
            new GlobalVoxelOffset(-1,1,-1),
            new GlobalVoxelOffset(-1,1,0),
            new GlobalVoxelOffset(-1,1,1),

            new GlobalVoxelOffset(0,-1,-1),
            new GlobalVoxelOffset(0,-1,0),
            new GlobalVoxelOffset(0,-1,1),
            new GlobalVoxelOffset(0,0,-1),
            //new GlobalVoxelOffset(0,0,0),
            new GlobalVoxelOffset(0,0,1),
            new GlobalVoxelOffset(0,1,-1),
            new GlobalVoxelOffset(0,1,0),
            new GlobalVoxelOffset(0,1,1),

            new GlobalVoxelOffset(1,-1,-1),
            new GlobalVoxelOffset(1,-1,0),
            new GlobalVoxelOffset(1,-1,1),
            new GlobalVoxelOffset(1,0,-1),
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(1,0,1),
            new GlobalVoxelOffset(1,1,-1),
            new GlobalVoxelOffset(1,1,0),
            new GlobalVoxelOffset(1,1,1),
        };

        private static GlobalVoxelOffset[] Cube = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(-1,-1,-1),
            new GlobalVoxelOffset(-1,-1,0),
            new GlobalVoxelOffset(-1,-1,1),
            new GlobalVoxelOffset(-1,0,-1),
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(-1,0,1),
            new GlobalVoxelOffset(-1,1,-1),
            new GlobalVoxelOffset(-1,1,0),
            new GlobalVoxelOffset(-1,1,1),

            new GlobalVoxelOffset(0,-1,-1),
            new GlobalVoxelOffset(0,-1,0),
            new GlobalVoxelOffset(0,-1,1),
            new GlobalVoxelOffset(0,0,-1),
            new GlobalVoxelOffset(0,0,0),
            new GlobalVoxelOffset(0,0,1),
            new GlobalVoxelOffset(0,1,-1),
            new GlobalVoxelOffset(0,1,0),
            new GlobalVoxelOffset(0,1,1),

            new GlobalVoxelOffset(1,-1,-1),
            new GlobalVoxelOffset(1,-1,0),
            new GlobalVoxelOffset(1,-1,1),
            new GlobalVoxelOffset(1,0,-1),
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(1,0,1),
            new GlobalVoxelOffset(1,1,-1),
            new GlobalVoxelOffset(1,1,0),
            new GlobalVoxelOffset(1,1,1),
        };

        #region VertexNeighbors
        public static readonly GlobalVoxelOffset[][] VertexNeighbors = new GlobalVoxelOffset[][]
        {
            new GlobalVoxelOffset[] // Front Top Left (-1, 1, 1)
            {
                new GlobalVoxelOffset(-1,0,0),
                new GlobalVoxelOffset(-1,0,1),
                new GlobalVoxelOffset(-1,1,0),
                new GlobalVoxelOffset(-1,1,1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1),
                new GlobalVoxelOffset(0,1,0),
                new GlobalVoxelOffset(0,1,1)
            },

            new GlobalVoxelOffset[] // Front Top Right (1, 1, 1)
            {
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1),
                new GlobalVoxelOffset(0,1,0),
                new GlobalVoxelOffset(0,1,1),
                new GlobalVoxelOffset(1,0,0),
                new GlobalVoxelOffset(1,0,1),
                new GlobalVoxelOffset(1,1,0),
                new GlobalVoxelOffset(1,1,1)
            },

            new GlobalVoxelOffset[] // Front Bottom Left (-1, -1, 1)
            {
                new GlobalVoxelOffset(-1,-1,0),
                new GlobalVoxelOffset(-1,-1,1),
                new GlobalVoxelOffset(-1,0,0),
                new GlobalVoxelOffset(-1,0,1),
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,-1,1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1)
            },

            new GlobalVoxelOffset[] // Front Bottom Right (1, -1, 1)
            {
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,-1,1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1),
                new GlobalVoxelOffset(1,-1,0),
                new GlobalVoxelOffset(1,-1,1),
                new GlobalVoxelOffset(1,0,0),
                new GlobalVoxelOffset(1,0,1)
            },

            new GlobalVoxelOffset[] // Back Top Left (-1, 1, -1)
            {
                new GlobalVoxelOffset(-1,0,-1),
                new GlobalVoxelOffset(-1,0,0),
                new GlobalVoxelOffset(-1,1,-1),
                new GlobalVoxelOffset(-1,1,0),
                new GlobalVoxelOffset(0,0,-1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,1,-1),
                new GlobalVoxelOffset(0,1,0)
            },

            new GlobalVoxelOffset[] // Back Top Right (1, 1, -1)
            {
                new GlobalVoxelOffset(0,0,-1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,1,-1),
                new GlobalVoxelOffset(0,1,0),
                new GlobalVoxelOffset(1,0,-1),
                new GlobalVoxelOffset(1,0,0),
                new GlobalVoxelOffset(1,1,-1),
                new GlobalVoxelOffset(1,1,0)
            },

            new GlobalVoxelOffset[] // Back Bottom Left (-1, -1, -1)
            {
                new GlobalVoxelOffset(-1,-1,-1),
                new GlobalVoxelOffset(-1,-1,0),
                new GlobalVoxelOffset(-1,0,-1),
                new GlobalVoxelOffset(-1,0,0),
                new GlobalVoxelOffset(0,-1,-1),
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,0,-1),
                new GlobalVoxelOffset(0,0,0)
            },

            new GlobalVoxelOffset[] // Back Bottom Right (1, -1, -1)
            {
                new GlobalVoxelOffset(0,-1,-1),
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,0,-1),
                new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(1,-1,-1),
                new GlobalVoxelOffset(1,-1,0),
                new GlobalVoxelOffset(1,0,-1),
                new GlobalVoxelOffset(1,0,0)
            },
        };
        #endregion

        #region Vertex Neighbors 2D
        private static GlobalVoxelOffset[][] VertexNeighbors2D = new GlobalVoxelOffset[][]
        {
            // Front Top Left
            new GlobalVoxelOffset[]
            {
                new GlobalVoxelOffset(-1, 0, 0),
                new GlobalVoxelOffset(-1, 0, 1),
                new GlobalVoxelOffset(0, 0, 1)
            },

            // Front Top Right
            new GlobalVoxelOffset[]
            {
                new GlobalVoxelOffset(0, 0, 1),
                new GlobalVoxelOffset(1, 0, 1),
                new GlobalVoxelOffset(1, 0, 0)
            },

            // Front Bottom Left
            new GlobalVoxelOffset[] { },

            // Front Bottom Right
            new GlobalVoxelOffset[] { },

            // Back Top Left
            new GlobalVoxelOffset[]
            {
                new GlobalVoxelOffset(-1, 0, 0),
                new GlobalVoxelOffset(-1, 0, -1),
                new GlobalVoxelOffset(0, 0, -1)
            },
            
            // Back Top Right
            new GlobalVoxelOffset[]
            {
                new GlobalVoxelOffset(0, 0, -1),
                new GlobalVoxelOffset(1, 0, -1),
                new GlobalVoxelOffset(1, 0, 0)
            },

            // Back Bottom Left
            new GlobalVoxelOffset[] { },

            // Back Bottom Right
            new GlobalVoxelOffset[] { }
        };
        #endregion

        #endregion

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateNeighbors(
            IEnumerable<GlobalVoxelOffset> Neighbors,
            GlobalVoxelCoordinate Coordinate)
        {
            foreach (var offset in Neighbors)
                yield return Coordinate + offset;
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateManhattanNeighbors(
            GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateManhattanNeighbors2D(
            GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors2D, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateManhattanNeighbors2D(
           GlobalVoxelCoordinate Coordinate, ChunkManager.SliceMode SliceMode)
        {
            switch (SliceMode)
            {
                case ChunkManager.SliceMode.X:
                    return EnumerateNeighbors(ManhattanNeighbors2D.Select(n =>
                        new GlobalVoxelOffset(0, n.Z, n.X)), Coordinate);
                case ChunkManager.SliceMode.Y:
                    return EnumerateNeighbors(ManhattanNeighbors2D, Coordinate);
                case ChunkManager.SliceMode.Z:
                    return EnumerateNeighbors(ManhattanNeighbors2D.Select(n =>
                        new GlobalVoxelOffset(n.X, n.Z, 0)), Coordinate);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateAllNeighbors(
            GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(AllNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateCube(
            GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(Cube, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateManhattanCube(
           GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanCubeNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateVertexNeighbors(
            GlobalVoxelCoordinate Coordinate, VoxelVertex Vertex)
        {
            return EnumerateNeighbors(VertexNeighbors[(int)Vertex], Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateVertexNeighbors2D(
            GlobalVoxelCoordinate Coordinate, VoxelVertex Vertex)
        {
            return EnumerateNeighbors(VertexNeighbors2D[(int)Vertex], Coordinate);
        }

        public static TemporaryVoxelHandle GetNeighbor(TemporaryVoxelHandle Of, GlobalVoxelOffset Offset)
        {
            if (!Of.IsValid) return TemporaryVoxelHandle.InvalidHandle;
            return new TemporaryVoxelHandle(Of.Chunk.Manager.ChunkData, Of.Coordinate + Offset);
        }
    }
}
