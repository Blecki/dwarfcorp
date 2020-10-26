using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpVoxReader.Chunks
{
    public class Unknown : Chunk
    {
        private string _Id;

        public Unknown(string id)
        {
            _Id = id;
        }

        internal override string Id
        {
            get
            {
                return _Id;
            }
        }

        internal override int Read(System.IO.BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            br.ReadBytes(Size);
            br.ReadBytes(ChildrenSize);
            return readSize + Size + ChildrenSize;
        }
    }
}
