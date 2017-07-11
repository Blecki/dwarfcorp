using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class VoxelData
    {
        public bool[] IsExplored;
        public byte[] Health;
        public byte[] Types;
        public byte[] SunColors;
        public WaterCell[] Water;
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public RampType[] RampTypes;

        public int CornerIndexAt(int x, int y, int z)
        {
            return (z * (SizeY + 1) + y) * (SizeX + 1) + x;
        }

        public int IndexAt(LocalVoxelCoordinate C)
        {
            return (C.Z * SizeY + C.Y) * SizeX + C.X;
        }

        public Vector3 CoordsAt(int idx)
        {
            int x = idx % (SizeX);
            idx /= (SizeX);
            int y = idx % (SizeY);
            idx /= (SizeY);
            int z = idx;
            return new Vector3(x, y, z);
        }
    }

}
