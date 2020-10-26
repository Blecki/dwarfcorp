using System;
using System.IO;

namespace CsharpVoxReader.Chunks
{
    public class Model : Chunk
    {
        public const string ID = "XYZI";

        private byte[,,] _Indexes;

        internal override string Id
        {
            get { return ID; }
        }

        public byte[,,] Indexes
        {
            get { return _Indexes; }
        }

        internal override int Read(BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            Int32 numVoxels = br.ReadInt32();
            readSize += sizeof(Int32);

            for (int i = 0; i < numVoxels; i++)
            {
                byte x = br.ReadByte();
                byte z = br.ReadByte();
                byte y = br.ReadByte();
                byte index = br.ReadByte();
                _Indexes[x, y, z] = index;
                readSize += 4;
            }
            return readSize;
        }

        public void Init(int sizeX, int sizeY, int sizeZ)
        {
            _Indexes = new byte[sizeX, sizeY, sizeZ];
        }
    }
}
