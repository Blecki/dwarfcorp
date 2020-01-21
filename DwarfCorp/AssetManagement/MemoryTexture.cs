using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DwarfCorp
{
    public class MemoryTexture
    {
        public Color[] Data { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public MemoryTexture(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
            this.Data = new Color[Width * Height];
        }

        public int Index(int X, int Y)
        {
            return (Y * Width) + X;
        }

        public void Filter(Func<Color, Color> FilterFunction)
        {
            for (var i = 0; i < Data.Length; ++i)
                Data[i] = FilterFunction(Data[i]);
        }
    }
}