using System;
using System.IO;
using System.Linq;

namespace CsharpVoxReader.Chunks
{
    public class Main : Chunk
    {
        public const string ID = "MAIN";

        internal override string Id
        {
            get { return ID; }
        }

        internal override int Read(BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            if(Size > 0)
            {
                throw new InvalidDataException($"MAIN chunk size is expected to be 0 (was {Size}");
            }

            int childrenReadSize = 0;

            while (childrenReadSize < ChildrenSize)
            {
                string id = Chunk.ReadChunkId(br);
                Chunk child = Chunk.CreateChunk(id);
                childrenReadSize += child.Read(br, loader) + 4;
            }

            return readSize + Size + ChildrenSize;
        }
    }
}


