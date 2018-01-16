using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public enum DecalOrientation
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public class DecalType
    {
        public static DecalOrientation RotateOrientation(DecalOrientation Orientation, int Amount)
        {
            return (DecalOrientation)(((int)Orientation + Amount) % 4);
        }

        public static byte DecodeDecalType(byte RawData)
        {
            return (byte)(RawData & 0x3F);
        }

        public static DecalOrientation DecodeDecalOrientation(byte RawData)
        {
            return (DecalOrientation)((RawData & 0xC0) >> 6);
        }

        public static byte EncodeDecal(DecalOrientation Orientation, byte Type)
        {
            return (byte)((((byte)Orientation) << 6) + (Type & 0x3F));
        }

        public byte ID;
        public String Name;
        public Point Tile;
    }
}
