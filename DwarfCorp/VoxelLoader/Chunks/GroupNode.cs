using System;
using System.IO;
using System.Collections.Generic;

namespace CsharpVoxReader.Chunks
{
    public class GroupNode : Chunk
    {
        public const string ID = "nGRP";

        internal override string Id
        {
            get { return ID; }
        }

        internal override int Read(BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            Int32 id = br.ReadInt32();
            Dictionary<string, byte[]> attributes = GenericsReader.ReadDict(br, ref readSize);

            Int32 numChildrenNodes = br.ReadInt32();
            Int32[] childrenIds = new Int32[numChildrenNodes];
            readSize += sizeof(Int32) * (numChildrenNodes + 2);

            for (int cnum=0; cnum < numChildrenNodes; cnum++) {
              childrenIds[cnum] = br.ReadInt32();
            }

            loader.NewGroupNode(id, attributes, childrenIds);
            return readSize;
        }
    }
}
