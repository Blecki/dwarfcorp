using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui
{
    public partial class Mesh
    {
        public static Mesh CreateStringMesh(
            String String, 
            ITileSheet FontSheet, 
            Vector2 GlyphScale,
            out Rectangle Bounds)
        {
            var glyphMeshes = new List<Mesh>();
            var pos = Vector2.Zero;
            var maxX = 0.0f;

            foreach (var c in String)
            {
                if (c == '\n')
                {
                    if (pos.X > maxX) maxX = pos.X;
                    pos.X = 0;
                    pos.Y += FontSheet.TileHeight * GlyphScale.Y;
                }
                else if (c < 32) continue;
                else
                {
                    var x = c;
                    if (!FontSheet.HasGlyph(c - ' '))
                        x = '*';

                    var glyphSize = FontSheet.GlyphSize(x - ' ');
                    glyphMeshes.Add(Mesh.Quad()
                        .Texture(FontSheet.TileMatrix(x - ' '))
                        .Scale(glyphSize.X * GlyphScale.X, glyphSize.Y * GlyphScale.Y)
                        .Translate(pos.X, pos.Y));
                    pos.X += glyphSize.X * GlyphScale.X;
                }
            }

            Bounds = new Rectangle(0, 0, (int)global::System.Math.Max(maxX, pos.X), (int)(pos.Y + ((FontSheet.TileHeight * GlyphScale.Y))));

            return Merge(glyphMeshes.ToArray());
        }

    }
}