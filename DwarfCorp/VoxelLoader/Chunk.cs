using System;
using System.IO;
using System.Collections.Generic;

namespace CsharpVoxReader
{
    public abstract class Chunk
    {
        private Int32 _Size;
        private Int32 _ChildrenSize;

        internal abstract string Id { get; }

        internal Int32 Size
        {
            get { return _Size; }
        }

        internal Int32 ChildrenSize
        {
            get { return _ChildrenSize; }
        }

        internal static string ReadChunkId(BinaryReader br)
        {
            if (br == null) throw new ArgumentNullException(nameof(br));

            char[] id = br.ReadChars(4);
            return new string(id);
        }

        internal virtual int Read(BinaryReader br, IVoxLoader loader)
        {
            if (br == null) throw new ArgumentNullException(nameof(br));
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            _Size = br.ReadInt32();
            _ChildrenSize = br.ReadInt32();

            return sizeof(Int32) * 2;
        }

        internal static Chunk CreateChunk(string id)
        {
            switch (id)
            {
                case Chunks.Main.ID : return new Chunks.Main();
                case Chunks.Size.ID : return new Chunks.Size();
                case Chunks.Model.ID : return new Chunks.Model();
                case Chunks.Palette.ID : return new Chunks.Palette();
                case Chunks.Pack.ID: return new Chunks.Pack();
                case Chunks.MaterialOld.ID: return new Chunks.MaterialOld();
                case Chunks.TransformNode.ID: return new Chunks.TransformNode();
                case Chunks.GroupNode.ID: return new Chunks.GroupNode();
                case Chunks.ShapeNode.ID: return new Chunks.ShapeNode();
                case Chunks.Material.ID: return new Chunks.Material();
                case Chunks.Layer.ID: return new Chunks.Layer();
                default: return new Chunks.Unknown(id);
            }
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
