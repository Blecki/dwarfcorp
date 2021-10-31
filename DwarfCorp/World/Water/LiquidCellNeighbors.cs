using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public partial class LiquidCellHelpers
    {
        #region Offset Lists
        private static GlobalLiquidOffset[] ManhattanNeighbors = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(-1,0,0),
            new GlobalLiquidOffset(0,1,0),
            new GlobalLiquidOffset(0,-1,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(0,0,-1)
        };

        private static GlobalLiquidOffset[] ManhattanCubeNeighbors = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(0,0,0),
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(-1,0,0),
            new GlobalLiquidOffset(0,1,0),
            new GlobalLiquidOffset(0,-1,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(0,0,-1)
        };

        public static GlobalLiquidOffset[] ManhattanNeighbors2D = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(0,0,-1),
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(-1,0,0)
        };

        public static GlobalLiquidOffset[] DiagonalNeighbors2D = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(-1,0,-1),
            new GlobalLiquidOffset(1,0,-1),
            new GlobalLiquidOffset(1,0,1),
            new GlobalLiquidOffset(-1,0,1)
        };

        private static GlobalLiquidOffset[] AllNeighbors = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(-1,-1,-1),
            new GlobalLiquidOffset(-1,-1,0),
            new GlobalLiquidOffset(-1,-1,1),
            new GlobalLiquidOffset(-1,0,-1),
            new GlobalLiquidOffset(-1,0,0),
            new GlobalLiquidOffset(-1,0,1),
            new GlobalLiquidOffset(-1,1,-1),
            new GlobalLiquidOffset(-1,1,0),
            new GlobalLiquidOffset(-1,1,1),

            new GlobalLiquidOffset(0,-1,-1),
            new GlobalLiquidOffset(0,-1,0),
            new GlobalLiquidOffset(0,-1,1),
            new GlobalLiquidOffset(0,0,-1),
            //new GlobalLiquidOffset(0,0,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(0,1,-1),
            new GlobalLiquidOffset(0,1,0),
            new GlobalLiquidOffset(0,1,1),

            new GlobalLiquidOffset(1,-1,-1),
            new GlobalLiquidOffset(1,-1,0),
            new GlobalLiquidOffset(1,-1,1),
            new GlobalLiquidOffset(1,0,-1),
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(1,0,1),
            new GlobalLiquidOffset(1,1,-1),
            new GlobalLiquidOffset(1,1,0),
            new GlobalLiquidOffset(1,1,1),
        };

        private static GlobalLiquidOffset[] Cube = new GlobalLiquidOffset[]
        {
            new GlobalLiquidOffset(-1,-1,-1),
            new GlobalLiquidOffset(-1,-1,0),
            new GlobalLiquidOffset(-1,-1,1),
            new GlobalLiquidOffset(-1,0,-1),
            new GlobalLiquidOffset(-1,0,0),
            new GlobalLiquidOffset(-1,0,1),
            new GlobalLiquidOffset(-1,1,-1),
            new GlobalLiquidOffset(-1,1,0),
            new GlobalLiquidOffset(-1,1,1),

            new GlobalLiquidOffset(0,-1,-1),
            new GlobalLiquidOffset(0,-1,0),
            new GlobalLiquidOffset(0,-1,1),
            new GlobalLiquidOffset(0,0,-1),
            new GlobalLiquidOffset(0,0,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(0,1,-1),
            new GlobalLiquidOffset(0,1,0),
            new GlobalLiquidOffset(0,1,1),

            new GlobalLiquidOffset(1,-1,-1),
            new GlobalLiquidOffset(1,-1,0),
            new GlobalLiquidOffset(1,-1,1),
            new GlobalLiquidOffset(1,0,-1),
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(1,0,1),
            new GlobalLiquidOffset(1,1,-1),
            new GlobalLiquidOffset(1,1,0),
            new GlobalLiquidOffset(1,1,1),
        };

        #region VertexNeighbors
        public static readonly GlobalLiquidOffset[][] VertexNeighbors = new GlobalLiquidOffset[][]
        {
            // This MUST be in the same order as the VoxelVertex enum!

            new GlobalLiquidOffset[] // Back Bottom Left (-1, -1, -1)
            {
                new GlobalLiquidOffset(-1,-1,-1),
                new GlobalLiquidOffset(-1,-1,0),
                new GlobalLiquidOffset(-1,0,-1),
                new GlobalLiquidOffset(-1,0,0),
                new GlobalLiquidOffset(0,-1,-1),
                new GlobalLiquidOffset(0,-1,0),
                new GlobalLiquidOffset(0,0,-1),
                new GlobalLiquidOffset(0,0,0)
            },

            new GlobalLiquidOffset[] // Front Bottom Left (-1, -1, 1)
            {
                new GlobalLiquidOffset(-1,-1,0),
                new GlobalLiquidOffset(-1,-1,1),
                new GlobalLiquidOffset(-1,0,0),
                new GlobalLiquidOffset(-1,0,1),
                new GlobalLiquidOffset(0,-1,0),
                new GlobalLiquidOffset(0,-1,1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,0,1)
            },

            new GlobalLiquidOffset[] // Back Top Left (-1, 1, -1)
            {
                new GlobalLiquidOffset(-1,0,-1),
                new GlobalLiquidOffset(-1,0,0),
                new GlobalLiquidOffset(-1,1,-1),
                new GlobalLiquidOffset(-1,1,0),
                new GlobalLiquidOffset(0,0,-1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,1,-1),
                new GlobalLiquidOffset(0,1,0)
            },

            new GlobalLiquidOffset[] // Front Top Left (-1, 1, 1)
            {
                new GlobalLiquidOffset(-1,0,0),
                new GlobalLiquidOffset(-1,0,1),
                new GlobalLiquidOffset(-1,1,0),
                new GlobalLiquidOffset(-1,1,1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,0,1),
                new GlobalLiquidOffset(0,1,0),
                new GlobalLiquidOffset(0,1,1)
            },

            new GlobalLiquidOffset[] // Back Bottom Right (1, -1, -1)
            {
                new GlobalLiquidOffset(0,-1,-1),
                new GlobalLiquidOffset(0,-1,0),
                new GlobalLiquidOffset(0,0,-1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(1,-1,-1),
                new GlobalLiquidOffset(1,-1,0),
                new GlobalLiquidOffset(1,0,-1),
                new GlobalLiquidOffset(1,0,0)
            },

            new GlobalLiquidOffset[] // Front Bottom Right (1, -1, 1)
            {
                new GlobalLiquidOffset(0,-1,0),
                new GlobalLiquidOffset(0,-1,1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,0,1),
                new GlobalLiquidOffset(1,-1,0),
                new GlobalLiquidOffset(1,-1,1),
                new GlobalLiquidOffset(1,0,0),
                new GlobalLiquidOffset(1,0,1)
            },

            new GlobalLiquidOffset[] // Back Top Right (1, 1, -1)
            {
                new GlobalLiquidOffset(0,0,-1),
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,1,-1),
                new GlobalLiquidOffset(0,1,0),
                new GlobalLiquidOffset(1,0,-1),
                new GlobalLiquidOffset(1,0,0),
                new GlobalLiquidOffset(1,1,-1),
                new GlobalLiquidOffset(1,1,0)
            },

            new GlobalLiquidOffset[] // Front Top Right (1, 1, 1)
            {
                new GlobalLiquidOffset(0,0,0),
                new GlobalLiquidOffset(0,0,1),
                new GlobalLiquidOffset(0,1,0),
                new GlobalLiquidOffset(0,1,1),
                new GlobalLiquidOffset(1,0,0),
                new GlobalLiquidOffset(1,0,1),
                new GlobalLiquidOffset(1,1,0),
                new GlobalLiquidOffset(1,1,1)
            }
        };
        #endregion

        #region Vertex Neighbors 2D
        private static GlobalLiquidOffset[][] VertexNeighbors2D = new GlobalLiquidOffset[][]
        {
            // Back Bottom Left
            new GlobalLiquidOffset[] { },

            // Front Bottom Left
            new GlobalLiquidOffset[] { },

            // Back Top Left
            new GlobalLiquidOffset[]
            {
                new GlobalLiquidOffset(-1, 0, 0),
                new GlobalLiquidOffset(-1, 0, -1),
                new GlobalLiquidOffset(0, 0, -1),
                new GlobalLiquidOffset(0,0,0)
            },
            
            // Front Top Left
            new GlobalLiquidOffset[]
            {
                new GlobalLiquidOffset(-1, 0, 0),
                new GlobalLiquidOffset(-1, 0, 1),
                new GlobalLiquidOffset(0, 0, 1),
                new GlobalLiquidOffset(0,0,0)
            },
            
            // Back Bottom Right
            new GlobalLiquidOffset[] { },
            
            // Front Bottom Right
            new GlobalLiquidOffset[] { },
            
            // Back Top Right
            new GlobalLiquidOffset[]
            {
                new GlobalLiquidOffset(0, 0, -1),
                new GlobalLiquidOffset(1, 0, -1),
                new GlobalLiquidOffset(1, 0, 0),
                new GlobalLiquidOffset(0,0,0)
            },

            // Front Top Right
            new GlobalLiquidOffset[]
            {
                new GlobalLiquidOffset(0, 0, 1),
                new GlobalLiquidOffset(1, 0, 1),
                new GlobalLiquidOffset(1, 0, 0),
                new GlobalLiquidOffset(0,0,0)
            },
        };
        #endregion

        #endregion

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateNeighbors(
            IEnumerable<GlobalLiquidOffset> Neighbors,
            GlobalLiquidCoordinate Coordinate)
        {
            foreach (var offset in Neighbors)
                yield return Coordinate + offset;
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanNeighbors(
            GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanNeighbors2D(
            GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors2D, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanNeighbors2D_Y(GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors2D, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanNeighbors2D_X(GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors2D.Select(n => new GlobalLiquidOffset(0, -n.Z, n.X)), Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanNeighbors2D_Z(GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanNeighbors2D.Select(n => new GlobalLiquidOffset(n.X, -n.Z, 0)), Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateAllNeighbors(
            GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(AllNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateCube(
            GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(Cube, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateManhattanCube(
           GlobalLiquidCoordinate Coordinate)
        {
            return EnumerateNeighbors(ManhattanCubeNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateVertexNeighbors(
            GlobalLiquidCoordinate Coordinate, VoxelVertex Vertex)
        {
            return EnumerateNeighbors(VertexNeighbors[(int)Vertex], Coordinate);
        }

        public static IEnumerable<GlobalLiquidCoordinate> EnumerateVertexNeighbors2D(
            GlobalLiquidCoordinate Coordinate, VoxelVertex Vertex)
        {
            return EnumerateNeighbors(VertexNeighbors2D[(int)Vertex], Coordinate);
        }

        public static LiquidCellHandle GetNeighbor(LiquidCellHandle Of, GlobalLiquidOffset Offset)
        {
            if (!Of.IsValid) return LiquidCellHandle.InvalidHandle;
            return new LiquidCellHandle(Of.Chunk.Manager, Of.Coordinate + Offset);
        }
    }
}
