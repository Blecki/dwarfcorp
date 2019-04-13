using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp;

namespace SVT
{
    public class Level2
    {
        public MiniPoint3 Origin;
        private int[] RawBuffer;
        private int Voxel;

        public Level2(MiniPoint3 Min, int Voxel)
        {
            Origin = Min;
            this.Voxel = Voxel;
        }

        private void Subdivide()
        {
            RawBuffer = new int[8];
            for (var x = 0; x < 8; ++x)
                RawBuffer[x] = Voxel;
        }

        public int GetVoxel(MiniPoint3 Coordinate)
        {
            if (RawBuffer == null)
                return Voxel;
            return RawBuffer[EncodeIndex(Coordinate.X - Origin.X, Coordinate.Y - Origin.Y, Coordinate.Z - Origin.Z)];
        }

        public void SetVoxel(MiniPoint3 Coordinate, int Voxel)
        {
            if (RawBuffer == null)
            {
                if (this.Voxel == Voxel)
                    return;
                Subdivide();
            }

            RawBuffer[EncodeIndex(Coordinate.X - Origin.X, Coordinate.Y - Origin.Y, Coordinate.Z - Origin.Z)] = Voxel;
        }

        public Tuple<int, int> CalculateMemoryUsage()
        {
            return Tuple.Create(7, 8);
        }

        public static int EncodeIndex(int X, int Y, int Z)
        {
            return (Z << 2) + (Y << 1) + X;
        }

        public override string ToString()
        {
            return String.Format("L2 {0} {1} {2}", Origin.X, Origin.Y, Origin.Z);
        }
    }

    public class Level4
    {
        private MiniPoint3 Origin;
        private Level2[] Children;
        private int Voxel;

        public Level4(MiniPoint3 Origin, int Voxel)
        {
            this.Origin = Origin;
            this.Voxel = Voxel;
        }

        private void Subdivide()
        {
            Children = new Level2[8]
            {
                /*000*/ new Level2(Origin, Voxel),
                /*001*/ new Level2(new MiniPoint3((byte)(Origin.X + 2), Origin.Y, Origin.Z), Voxel),
                /*010*/ new Level2(new MiniPoint3(Origin.X, (byte)(Origin.Y + 2), Origin.Z), Voxel),
                /*011*/ new Level2(new MiniPoint3((byte)(Origin.X + 2), (byte)(Origin.Y + 2), Origin.Z), Voxel),

                /*100*/ new Level2(new MiniPoint3(Origin.X, Origin.Y, (byte)(Origin.Z + 2)), Voxel),
                /*101*/ new Level2(new MiniPoint3((byte)(Origin.X + 2), Origin.Y, (byte)(Origin.Z + 2)), Voxel),
                /*110*/ new Level2(new MiniPoint3(Origin.X, (byte)(Origin.Y + 2), (byte)(Origin.Z + 2)), Voxel),
                /*111*/ new Level2(new MiniPoint3((byte)(Origin.X + 2), (byte)(Origin.Y + 2), (byte)(Origin.Z + 2)), Voxel)
            };
        }

        public int GetVoxel(MiniPoint3 Coordinate)
        {
            if (Children == null)
                return Voxel;

            return Children[EncodeIndex(Coordinate)].GetVoxel(Coordinate);
        }

        public void SetVoxel(MiniPoint3 Coordinate, int Voxel)
        {
            if (Children == null)
            {
                if (this.Voxel == Voxel) return;
                Subdivide();
            }

            Children[EncodeIndex(Coordinate)].SetVoxel(Coordinate, Voxel);
        }

        private int EncodeIndex(MiniPoint3 Coordinate)
        {
            var x = ((Coordinate.X - Origin.X) >> 1) & 0x1;
            var y = ((Coordinate.Y - Origin.Y) >> 1) & 0x1;
            var z = ((Coordinate.Z - Origin.Z) >> 1) & 0x1;

            return Level2.EncodeIndex(x, y, z);
        }
    }

    public class Level8
    {
        private MiniPoint3 Origin;
        private Level4[] Children;
        private int Voxel;

        public Level8(MiniPoint3 Origin, int Voxel)
        {
            this.Origin = Origin;
            this.Voxel = Voxel;
        }

        private void Subdivide()
        {
            Children = new Level4[8]
            {
                /*000*/ new Level4(Origin, Voxel),
                /*001*/ new Level4(new MiniPoint3((byte)(Origin.X + 4), Origin.Y, Origin.Z), Voxel),
                /*010*/ new Level4(new MiniPoint3(Origin.X, (byte)(Origin.Y + 4), Origin.Z), Voxel),
                /*011*/ new Level4(new MiniPoint3((byte)(Origin.X + 4), (byte)(Origin.Y + 4), Origin.Z), Voxel),

                /*100*/ new Level4(new MiniPoint3(Origin.X, Origin.Y, (byte)(Origin.Z + 4)), Voxel),
                /*101*/ new Level4(new MiniPoint3((byte)(Origin.X + 4), Origin.Y, (byte)(Origin.Z + 4)), Voxel),
                /*110*/ new Level4(new MiniPoint3(Origin.X, (byte)(Origin.Y + 4), (byte)(Origin.Z + 4)), Voxel),
                /*111*/ new Level4(new MiniPoint3((byte)(Origin.X + 4), (byte)(Origin.Y + 4), (byte)(Origin.Z + 4)), Voxel)
            };
        }

        public int GetVoxel(MiniPoint3 Coordinate)
        {
            if (Children == null)
                return Voxel;

            return Children[EncodeIndex(Coordinate)].GetVoxel(Coordinate);
        }

        public void SetVoxel(MiniPoint3 Coordinate, int Voxel)
        {
            if (Children == null)
            {
                if (this.Voxel == Voxel) return;
                Subdivide();
            }

            Children[EncodeIndex(Coordinate)].SetVoxel(Coordinate, Voxel);
        }

        private int EncodeIndex(MiniPoint3 Coordinate)
        {
            var x = ((Coordinate.X - Origin.X) >> 2) & 0x1;
            var y = ((Coordinate.Y - Origin.Y) >> 2) & 0x1;
            var z = ((Coordinate.Z - Origin.Z) >> 2) & 0x1;

            return Level2.EncodeIndex(x, y, z);
        }
    }

    public class Level16
    {
        private MiniPoint3 Origin;
        private Level8[] Children;
        private int Voxel;

        public Level16(MiniPoint3 Origin, int Voxel)
        {
            this.Origin = Origin;
            this.Voxel = Voxel;
        }

        private void Subdivide()
        {
            Children = new Level8[8]
            {
                /*000*/ new Level8(Origin, Voxel),
                /*001*/ new Level8(new MiniPoint3((byte)(Origin.X + 8), Origin.Y, Origin.Z), Voxel),
                /*010*/ new Level8(new MiniPoint3(Origin.X, (byte)(Origin.Y + 8), Origin.Z), Voxel),
                /*011*/ new Level8(new MiniPoint3((byte)(Origin.X + 8), (byte)(Origin.Y + 8), Origin.Z), Voxel),

                /*100*/ new Level8(new MiniPoint3(Origin.X, Origin.Y, (byte)(Origin.Z + 8)), Voxel),
                /*101*/ new Level8(new MiniPoint3((byte)(Origin.X + 8), Origin.Y, (byte)(Origin.Z + 8)), Voxel),
                /*110*/ new Level8(new MiniPoint3(Origin.X, (byte)(Origin.Y + 8), (byte)(Origin.Z + 8)), Voxel),
                /*111*/ new Level8(new MiniPoint3((byte)(Origin.X + 8), (byte)(Origin.Y + 8), (byte)(Origin.Z + 8)), Voxel)
            };
        }

        public int GetVoxel(MiniPoint3 Coordinate)
        {
            if (Children == null)
                return Voxel;

            return Children[EncodeIndex(Coordinate)].GetVoxel(Coordinate);
        }

        public void SetVoxel(MiniPoint3 Coordinate, int Voxel)
        {
            if (Children == null)
            {
                if (this.Voxel == Voxel) return;
                Subdivide();
            }

            Children[EncodeIndex(Coordinate)].SetVoxel(Coordinate, Voxel);
        }

        private int EncodeIndex(MiniPoint3 Coordinate)
        {
            var x = ((Coordinate.X - Origin.X) >> 3) & 0x1;
            var y = ((Coordinate.Y - Origin.Y) >> 3) & 0x1;
            var z = ((Coordinate.Z - Origin.Z) >> 3) & 0x1;

            return Level2.EncodeIndex(x, y, z);
        }
    }

    public class Level_TOP
    {
        private Level16[] Children;
        private int Voxel;

        public Level_TOP(int Voxel)
        {
            this.Voxel = Voxel;
        }

        private void Subdivide()
        {
            Children = new Level16[4]
            {
                /*000*/ new Level16(new MiniPoint3(0,0,0), Voxel),
                /*001*/ new Level16(new MiniPoint3(0,16,0), Voxel),
                /*010*/ new Level16(new MiniPoint3(0,32,0), Voxel),
                /*011*/ new Level16(new MiniPoint3(0,48,0), Voxel)
            };
        }

        public int GetVoxel(MiniPoint3 Coordinate)
        {
            if (Children == null)
                return Voxel;

            return Children[EncodeIndex(Coordinate)].GetVoxel(Coordinate);
        }

        public void SetVoxel(MiniPoint3 Coordinate, int Voxel)
        {
            if (Children == null)
            {
                if (this.Voxel == Voxel) return;
                Subdivide();
            }

            Children[EncodeIndex(Coordinate)].SetVoxel(Coordinate, Voxel);
        }

        private int EncodeIndex(MiniPoint3 Coordinate)
        {
            return (Coordinate.Y >> 4) & 0x3;
        }
    }
}
