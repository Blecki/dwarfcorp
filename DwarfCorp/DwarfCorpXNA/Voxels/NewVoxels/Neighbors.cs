using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Neighbors
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

        private static GlobalVoxelOffset[] ManhattanNeighbors2D = new GlobalVoxelOffset[]
        {
            new GlobalVoxelOffset(1,0,0),
            new GlobalVoxelOffset(-1,0,0),
            new GlobalVoxelOffset(0,0,1),
            new GlobalVoxelOffset(0,0,-1)
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

        private static GlobalVoxelOffset[][] VertexNeighbors = new GlobalVoxelOffset[][]
        {
            new GlobalVoxelOffset[] // Front Top Left (-1, 1, 1)
            {
                new GlobalVoxelOffset(-1,0,0),
                new GlobalVoxelOffset(-1,0,1),
                new GlobalVoxelOffset(-1,1,0),
                new GlobalVoxelOffset(-1,1,1),
                //new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1),
                new GlobalVoxelOffset(0,1,0),
                new GlobalVoxelOffset(0,1,1)
            },

            new GlobalVoxelOffset[] // Front Top Right (1, 1, 1)
            {
                //new GlobalVoxelOffset(0,0,0),
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
                //new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,0,1)
            },

            new GlobalVoxelOffset[] // Front Bottom Right (1, -1, 1)
            {
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,-1,1),
                //new GlobalVoxelOffset(0,0,0),
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
                //new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(0,1,-1),
                new GlobalVoxelOffset(0,1,0)
            },

            new GlobalVoxelOffset[] // Back Top Right (1, 1, -1)
            {
                new GlobalVoxelOffset(0,0,-1),
                //new GlobalVoxelOffset(0,0,0),
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
                //new GlobalVoxelOffset(0,0,0)
            },

            new GlobalVoxelOffset[] // Back Bottom Right (1, -1, -1)
            {
                new GlobalVoxelOffset(0,-1,-1),
                new GlobalVoxelOffset(0,-1,0),
                new GlobalVoxelOffset(0,0,-1),
                //new GlobalVoxelOffset(0,0,0),
                new GlobalVoxelOffset(1,-1,-1),
                new GlobalVoxelOffset(1,-1,0),
                new GlobalVoxelOffset(1,0,-1),
                new GlobalVoxelOffset(1,0,0)
            },
        };
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

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateAllNeighbors(
            GlobalVoxelCoordinate Coordinate)
        {
            return EnumerateNeighbors(AllNeighbors, Coordinate);
        }

        public static IEnumerable<GlobalVoxelCoordinate> EnumerateVertexNeighbors(
            GlobalVoxelCoordinate Coordinate, VoxelVertex Vertex)
        {
            return EnumerateNeighbors(VertexNeighbors[(int)Vertex], Coordinate);
        }
    }
}
