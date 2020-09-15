using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TextGrid : Widget
    {
        private Mesh GridMesh;
        public int TextWidth { get; private set; }
        public int TextHeight { get; private set; }
        public float CharacterScale = 1.0f;

        /// <summary>
        /// Trim a number of pixels off the sides of the character.
        /// </summary>
        public float MonoKerning = 0.0f;

        public override void Construct()
        {
            OnLayout += _OnLayout;
        }

        private void _OnLayout(Widget Sender)
        {
            var interior = GetDrawableInterior();
            var font = Root.GetTileSheet(Font);
            var glyphHeight = font.TileHeight * TextSize * CharacterScale;
            var glyphWidth = (font.TileHeight * TextSize * CharacterScale) - (MonoKerning * CharacterScale);
            var glyphHorizontalAdjust = (TextSize * MonoKerning * CharacterScale) / 2;

            var gridW = (int)(interior.Width / glyphWidth);
            var gridH = (int)(interior.Height / glyphHeight);

            var realX = interior.X + (interior.Width / 2) - ((glyphWidth * gridW) / 2);
            var realY = interior.Y + (interior.Height / 2) - ((glyphHeight * gridH) / 2);

            GridMesh = Mesh.EmptyMesh();
            for (var y = 0; y < gridH; ++y)
                for (var x = 0; x < gridW; ++x)
                    GridMesh.QuadPart()
                        // Scale to the actual expanded size of the glyph.
                        .Scale(font.TileWidth * TextSize * CharacterScale, font.TileHeight * TextSize * CharacterScale) 
                        // Position using the horizontal offset.
                        .Translate((x * glyphWidth) + realX - glyphHorizontalAdjust,
                            (y * glyphHeight) + realY)
                        .Texture(font.TileMatrix(0));

            TextWidth = gridW;
            TextHeight = gridH;
        }

        public void SetCharacter(int Index, char C)
        {
            if ((Index * 4) + 3 <= GridMesh.VertexCount)
                GridMesh.GetPart(Index * 4, 4)
                    .ResetQuadTexture()
                    .Texture(Root.GetTileSheet(Font).TileMatrix(C))
                    .Colorize(TextColor);
        }

        public void SetString(String S)
        {
            for (int i = 0; i < S.Length; ++i)
                SetCharacter(i, S[i]);
        }

        protected override Mesh Redraw()
        {
            var mesh = base.Redraw();
            mesh.Concat(GridMesh);
            return mesh;
        }
    }
}
