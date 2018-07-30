using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FontBuilder
{
    public class Range
    {
        public int Low = 0;
        public int High = 0;
    }

    public class Target
    {
        public String BaseFont = "";
        public int FontSize = 16;
        public String OutputName = "";
    }

    public class Options
    {
        public bool SearchForCharacters = true;
        public String SearchPath = "";
        public List<String> SearchExtensions = new List<string>();
        public String FontName = "Arial";
        public List<Range> Ranges = new List<Range> { new Range { Low = ' ', High = 128 } };
        public List<Target> Targets;
    }

    public class Glyph
    {
        public char Code;
        public int X;
        public int Y;
        public int Width;
        public int Height;

        [JsonIgnore]
        public System.Drawing.Bitmap Bitmap;
    }

    public class Atlas
    {
        public Rectangle Dimensions;
        public List<Glyph> Glyphs;
    }

    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int X, int Y, int Width, int Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }
    }
}
