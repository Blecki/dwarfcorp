using System;
using System.IO;
using System.Collections.Generic;

namespace CsharpVoxReader.Chunks
{
    public class TransformNode : Chunk
    {
        public const string ID = "nTRN";

        internal override string Id
        {
            get { return ID; }
        }

        internal override int Read(BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            Int32 id = br.ReadInt32();
            Dictionary<string, byte[]> attributes = GenericsReader.ReadDict(br, ref readSize);

            Int32 childNodeId = br.ReadInt32();
            Int32 reservedId = br.ReadInt32();
            Int32 layerId = br.ReadInt32();
            Int32 numOfFrames = br.ReadInt32();

            readSize += sizeof(Int32) * 5;

            Dictionary<string, byte[]>[] framesAttributes = new Dictionary<string, byte[]>[numOfFrames];

            for (int fnum=0; fnum < numOfFrames; fnum++) {
              framesAttributes[fnum] = GenericsReader.ReadDict(br, ref readSize);
              /*int[] rotationMatrix = GenericsReader.ReadRotation(br, ref readSize);
              int[] translationVector = { 0, 0, 0 };
              translationVector[0] = br.ReadInt32();
              translationVector[1] = br.ReadInt32();
              translationVector[2] = br.ReadInt32();*/
              // TODO: Add frame info
            }

            // TODO: Notify the IVoxLoader of the transform node
            loader.NewTransformNode(id, childNodeId, layerId, framesAttributes);
            return readSize;
        }
    }
}
