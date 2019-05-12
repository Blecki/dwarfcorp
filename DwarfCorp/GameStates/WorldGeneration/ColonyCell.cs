using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ColonyCell
    {
        public Rectangle Bounds;

        public static List<ColonyCell> DeriveFromTexture(String Filename)
        {
            var texture = AssetManager.GetContentTexture(Filename);
            var rawTexture = TextureTool.MemoryTextureFromTexture2D(texture);

            var results = new Dictionary<Color, Rectangle>();
            for (var x = 0; x < rawTexture.Width; ++x)
                for (var y = 0; y < rawTexture.Height; ++y)
                {
                    var c = rawTexture.Data[rawTexture.Index(x, y)];
                    if (!results.ContainsKey(c))
                        results.Add(c, new Rectangle(x, y, 1, 1));
                    else
                    {
                        var r = results[c];
                        if (x >= r.X + r.Width) r = new Rectangle(r.X, r.Y, x - r.X + 1, r.Height);
                        if (y >= r.Y + r.Height) r = new Rectangle(r.X, r.Y, r.Width, y - r.Y + 1);
                        results[c] = r;
                    }
                }

            return results.Values.Select(r => new ColonyCell { Bounds = r }).ToList();
        }
    }
}
