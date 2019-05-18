using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class IndexedTexture
    {
        public byte[] Data { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public IndexedTexture(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
            this.Data = new byte[Width * Height];
        }

        public int Index(int X, int Y)
        {
            return (Y * Width) + X;
        }
    }
}