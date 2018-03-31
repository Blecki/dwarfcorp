using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp;

namespace ABTest
{
    class SparseVoxelTree<T>
    {
        private DwarfCorp.Point3 Origin;
        private DwarfCorp.Point3 Dimensions;
        private DwarfCorp.Point3 SubdivisionFactors;

        private T Voxel;
        private SparseVoxelTree<T>[] Children;

        public SparseVoxelTree(Point3 Origin, Point3 Dimensions, Point3 SubdivisionFactors)
        {
            this.Origin = Origin;
            this.Dimensions = Dimensions;
            this.SubdivisionFactors = SubdivisionFactors;
        }

        public T GetVoxelAt(Point3 Coordinate)
        {
            return default(T);
        }

        public void SetVoxelAt(Point3 Coordinate, T Voxel)
        {

        }
    }
}
