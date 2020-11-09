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
        public MeshPart StringPart(
            String String,
            ITileSheet FontSheet,
            Vector2 GlyphScale,
            out Rectangle Bounds)
        {
            var pos = Vector2.Zero;
            var maxX = 0.0f;
            var prevChar = 0;

            var r = new MeshPart
            {
                Mesh = this,
                VertexOffset = this.VertexCount
            };

            foreach (var c in String)
            {
                if (c == '\n')
                {
                    if (pos.X > maxX) maxX = pos.X;
                    pos.X = 0;
                    pos.Y += FontSheet.TileHeight * GlyphScale.Y;
                    prevChar = 0;
                }
                else if (c < 32) continue;
                else
                {
                    var x = c;
                    if (!FontSheet.HasGlyph(c - ' '))
                        x = '*';
                    var kerning = FontSheet.GlyphKerning(prevChar, c - ' ');

                    var glyphSize = FontSheet.GlyphSize(x - ' ');
                    QuadPart()
                        .Texture(FontSheet.TileMatrix(x - ' '))
                        .Scale(glyphSize.X * GlyphScale.X, glyphSize.Y * GlyphScale.Y)
                        .Translate(pos.X - FontSheet.GlyphLeftBearing(c - ' ') + kerning, pos.Y);

                    pos.X += FontSheet.GlyphAdvance(c - ' ') * GlyphScale.X;
                    prevChar = c - ' ';
                }
            }

            Bounds = new Rectangle(0, 0, (int)global::System.Math.Max(maxX, pos.X), (int)(pos.Y + ((FontSheet.TileHeight * GlyphScale.Y))));
            r.VertexCount = this.VertexCount - r.VertexOffset;
            return r;
        }

        public static Rectangle MeasureStringMesh(
            String String,
            ITileSheet FontSheet,
            Vector2 GlyphScale)
        {
            var pos = Vector2.Zero;
            var maxX = 0.0f;
            var prevChar = 0;

            foreach (var c in String)
            {
                if (c == '\n')
                {
                    if (pos.X > maxX) maxX = pos.X;
                    pos.X = 0;
                    pos.Y += FontSheet.TileHeight * GlyphScale.Y;
                    prevChar = 0;
                }
                else if (c < 32) continue;
                else
                {
                    var x = c;
                    if (!FontSheet.HasGlyph(c - ' '))
                        x = '*';
                    var kerning = FontSheet.GlyphKerning(prevChar, c - ' ');

                    var glyphSize = FontSheet.GlyphSize(x - ' ');
                    pos.X += (kerning + FontSheet.GlyphAdvance(x - ' ')) * GlyphScale.X;
                    prevChar = c - ' ';
                }
            }

            return new Rectangle(0, 0, (int)global::System.Math.Max(maxX, pos.X), (int)(pos.Y + ((FontSheet.TileHeight * GlyphScale.Y))));
        }
    }
}