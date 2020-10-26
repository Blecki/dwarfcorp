using System;

namespace CsharpVoxReader.Chunks
{
    public class MaterialOld : Chunk
    {
        public const string ID = "MATT";

        internal override string Id
        {
            get { return ID; }
        }

        public enum MaterialTypes : Int32
        {
            Diffuse = 0,
            Metal = 1,
            Glass = 2,
            Emissive = 3
        };

        [Flags]
        public enum PropertyBits : UInt32
        {
            Plastic         = 1 << 0,
            Roughness       = 1 << 1,
            Specular        = 1 << 2,
            IOR             = 1 << 3,
            Attenuation     = 1 << 4,
            Power           = 1 << 5,
            Glow            = 1 << 6,
            IsTotalPower    = 1 << 7
        };

        internal override int Read(System.IO.BinaryReader br, IVoxLoader loader)
        {
            int readSize = base.Read(br, loader);

            Int32 paletteId = br.ReadInt32();
            Int32 type = br.ReadInt32();
            float weight = br.ReadSingle();
            UInt32 property = br.ReadUInt32();
            float normalized = br.ReadSingle();
            readSize += sizeof(Int32) * 2 + sizeof(UInt32) + sizeof(float) * 2;

            loader.SetMaterialOld(paletteId, (MaterialTypes)type, weight, (PropertyBits)property, normalized);

            return readSize;
        }
    }
}