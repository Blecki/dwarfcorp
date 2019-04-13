using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp;

namespace ABTest
{
    public class SparseVoxelTree
    {
        public IntegerBoundingBox Bounds;
        public SparseVoxelTree[] Children;
        private Point3 Mid;
        private int Voxel;
        private int[,,] RawBuffer;

        public SparseVoxelTree(Point3 Min, Point3 Max, int Voxel)
        {
            Bounds = new IntegerBoundingBox(Min, Max);
            Bounds.Max.X = Bounds.Min.X + NextPowerOfTwo(Bounds.Width);
            Bounds.Max.Y = Bounds.Min.Y + NextPowerOfTwo(Bounds.Height);
            Bounds.Max.Z = Bounds.Min.Z + NextPowerOfTwo(Bounds.Depth);

            Mid = new Point3(Min.X + Bounds.Width / 2, Min.Y + Bounds.Height / 2, Min.Z + Bounds.Depth / 2);

            this.Voxel = Voxel;

            if (Bounds.Width == 2 && Bounds.Height == 2 && Bounds.Depth == 2)
            {
                RawBuffer = new int[2, 2, 2];

                for (var x = 0; x < 2; ++x)
                    for (var y = 0; y < 2; ++y)
                        for (var z = 0; z < 2; ++z)
                            RawBuffer[x, y, z] = Voxel;
            }
        }

        private int NextPowerOfTwo(int N)
        {
            var r = 1;
            while (r < N)
                r <<= 1;
            return r;
        }

        private void Subdivide()
        {
            var Min = Bounds.Min;
            var Max = Bounds.Max;

            if (Bounds.Height == 64)
            {
                Children = new SparseVoxelTree[4]
                {
                    new SparseVoxelTree(Min, new Point3(Max.X, Min.Y + 16, Max.Z), Voxel),
                    new SparseVoxelTree(new Point3(Min.X, Min.Y + 16, Min.Z), new Point3(Max.X, Min.Y + 32, Max.Z), Voxel),
                    new SparseVoxelTree(new Point3(Min.X, Min.Y + 32, Min.Z), new Point3(Max.X, Min.Y + 48, Max.Z), Voxel),
                    new SparseVoxelTree(new Point3(Min.X, Min.Y + 48, Min.Z), new Point3(Max.X, Min.Y + 64, Max.Z), Voxel)
                };
            }
            else
            {
                Children = new SparseVoxelTree[8]
                {
                /*000*/ new SparseVoxelTree(new Point3(Min.X, Min.Y, Min.Z), new Point3(Mid.X, Mid.Y, Mid.Z), Voxel),
                /*001*/ new SparseVoxelTree(new Point3(Mid.X, Min.Y, Min.Z), new Point3(Max.X, Mid.Y, Mid.Z), Voxel),
                /*010*/ new SparseVoxelTree(new Point3(Min.X, Mid.Y, Min.Z), new Point3(Mid.X, Max.Y, Mid.Z), Voxel),
                /*011*/ new SparseVoxelTree(new Point3(Mid.X, Mid.Y, Min.Z), new Point3(Max.X, Max.Y, Mid.Z), Voxel),

                /*100*/ new SparseVoxelTree(new Point3(Min.X, Min.Y, Mid.Z), new Point3(Mid.X, Mid.Y, Max.Z), Voxel),
                /*101*/ new SparseVoxelTree(new Point3(Mid.X, Min.Y, Mid.Z), new Point3(Max.X, Mid.Y, Max.Z), Voxel),
                /*110*/ new SparseVoxelTree(new Point3(Min.X, Mid.Y, Mid.Z), new Point3(Mid.X, Max.Y, Max.Z), Voxel),
                /*111*/ new SparseVoxelTree(new Point3(Mid.X, Mid.Y, Mid.Z), new Point3(Max.X, Max.Y, Max.Z), Voxel)
                };
            }
        }

        public int GetVoxel(Point3 Coordinate)
        {
            if (RawBuffer != null)
                return RawBuffer[Coordinate.X - Bounds.Min.X, Coordinate.Y - Bounds.Min.Y, Coordinate.Z - Bounds.Min.Z];

            if (Children == null)
                return Voxel;
            
            foreach (var c in Children)
                if (c.Bounds.Contains(Coordinate))
                    return c.GetVoxel(Coordinate);

            return 0;
        }

        public void SetVoxel(Point3 Coordinate, int Voxel)
        {
            if (RawBuffer != null)
                RawBuffer[Coordinate.X - Bounds.Min.X, Coordinate.Y - Bounds.Min.Y, Coordinate.Z - Bounds.Min.Z] = Voxel;
            else
            {
                if (Children == null)
                {
                    if (this.Voxel != Voxel)
                        Subdivide();
                    else
                        return;
                }

                foreach (var c in Children)
                    if (c.Bounds.Contains(Coordinate))
                        c.SetVoxel(Coordinate, Voxel);
            }
        }        

        public Tuple<int, int> CalculateMemoryUsage()
        {
            var r = 24 + 12 + (4 * 5);
            var voxels = 1;
            if (RawBuffer != null)
            {
                r += 4;
                voxels += 8;
            }
            if (Children != null)
            {
                r += (4 * 8);
                foreach (var c in Children)
                {
                    var sum = c.CalculateMemoryUsage();
                    r += sum.Item1;
                    voxels += sum.Item2;
                }
            }
            return Tuple.Create(r, voxels);

        }
    }
}
