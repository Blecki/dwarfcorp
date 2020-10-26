using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CsharpVoxReader
{
    public class VoxReader
    {
        private const Int32 FILE_FORMAT_VERSION = 150;

        protected string Path { get; set; }
        protected IVoxLoader Loader { get; set; }
        protected Stream Origin { get; set; }

        public VoxReader(string path, IVoxLoader loader)
        {
            if ( ! File.Exists(path)) throw new FileNotFoundException("Can't open vox file : file not found", path);
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            Path = path;
            Loader = loader;
        }

        public VoxReader(Stream s, IVoxLoader loader)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            Origin = s;
            Loader = loader;
        }

        private VoxReader()
        {
        }

        public Chunk Read()
        {
            using (FileStream fs = File.OpenRead(Path)) {
              Origin = fs;
              return ReadFromStream();
            }
        }

        public Chunk ReadFromStream() {
          using(BinaryReader br = new BinaryReader(Origin))
          {
              char[] magicNumber = br.ReadChars(4);
              if( ! magicNumber.SequenceEqual("VOX ".ToCharArray()))
              {
                  throw new InvalidDataException("Can't read VOX file : invalid vox signature");
              }

              Int32 version = br.ReadInt32();
              if(version > FILE_FORMAT_VERSION)
              {
                  throw new InvalidDataException($"Can't read VOX file : file format version ({version}) is newer than reader version ({FILE_FORMAT_VERSION})");
              }

              string id = Chunk.ReadChunkId(br);
              if(id != Chunks.Main.ID)
              {
                  throw new InvalidDataException($"Can't read VOX file : MAIN chunk expected (was {id}");
              }

              Chunk main = Chunk.CreateChunk(id);
              main.Read(br, Loader);

              return main;
          }
        }
    }
}
