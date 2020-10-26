using System;

namespace CsharpVoxReader.Chunks
{
    public class Pack : Chunk
    {
        public const string ID = "PACK";

        internal override string Id
        {
            get { return ID; }
        }

        
        internal override int Read(System.IO.BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            Int32 numModels = br.ReadInt32();
            readSize += sizeof(Int32);

            loader.SetModelCount(numModels);

            return readSize;
        }
    }
}