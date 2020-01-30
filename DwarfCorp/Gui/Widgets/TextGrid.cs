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

        public override void Construct()
        {
            OnLayout += _OnLayout;
        }

        private void _OnLayout(Widget Sender)
        {
            var interior = GetDrawableInterior();
            var font = Root.GetTileSheet(Font);

            var gridW = (int)(interior.Width / (font.TileWidth * TextSize));
            var gridH = (int)(interior.Height / (font.TileHeight * TextSize));

            var realX = interior.X + (interior.Width / 2) - ((font.TileWidth * TextSize * gridW) / 2);
            var realY = interior.Y + (interior.Height / 2) - ((font.TileHeight * TextSize * gridH) / 2);

            GridMesh = Mesh.EmptyMesh();
            for (var y = 0; y < gridH; ++y)
                for (var x = 0; x < gridW; ++x)
                    GridMesh.QuadPart()
                        .Scale(font.TileWidth * TextSize, font.TileHeight * TextSize)
                        .Translate((x * font.TileWidth * TextSize) + realX,
                            (y * font.TileHeight * TextSize) + realY)
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
