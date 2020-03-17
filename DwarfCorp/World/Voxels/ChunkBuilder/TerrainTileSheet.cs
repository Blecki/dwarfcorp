using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels
{
    public class TerrainTileSheet
    {
        public const float BoundsFudge = 0.001f;

        public int TextureWidth { get; set; }
        public int TextureHeight { get; set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }

        public int Columns { get { return TextureWidth / TileWidth; } }
        public int Rows { get { return TextureHeight / TileHeight; } }
        public int Row(int TileIndex) { return TileIndex / Columns; }
        public int Column(int TileIndex) { return TileIndex % Columns; }
        public float TileUStep { get { return 1.0f / Columns; } }
        public float TileVStep { get { return 1.0f / Rows; } }
        public float ColumnU(int Column) { return TileUStep * Column; }
        public float RowV(int Row) { return TileVStep * Row; }
        public float TileU(int TileIndex) { return ColumnU(Column(TileIndex)); }
        public float TileV(int TileIndex) { return RowV(Row(TileIndex)); }

        // Generate UV transform matricies that align the UV range 0..1 to a tile.
        public Matrix ScaleMatrix { get { return Matrix.CreateScale(TileUStep, TileVStep, 1.0f); } }
        public Matrix TranslationMatrix(int Column, int Row) { return Matrix.CreateTranslation(ColumnU(Column), RowV(Row), 0.0f); }
        public Matrix TileMatrix(int Column, int Row) { return ScaleMatrix * TranslationMatrix(Column % Columns, Row % Rows); }
        public Matrix TileMatrix(int TileIndex) { return TileMatrix(Column(TileIndex), Row(TileIndex)); }
        public Matrix TileMatrix(int TileIndex, int ColumnSpan, int RowSpan)
        {
            return Matrix.CreateScale(ColumnSpan, RowSpan, 1.0f) * TileMatrix(TileIndex);
        }

        public Matrix TileMatrix(int Column, int Row, int ColumnSpan, int RowSpan)
        {
            return Matrix.CreateScale(ColumnSpan, RowSpan, 1.0f) * TileMatrix(Column, Row);
        }

        public TerrainTileSheet(int TextureWidth, int TextureHeight, int TileWidth, int TileHeight)
        {
            this.TextureWidth = TextureWidth;
            this.TextureHeight = TextureHeight;
            this.TileWidth = TileWidth;
            this.TileHeight = TileHeight;
        }

        public Vector2 MapTileUVs(Vector2 TextureCoord, Point Tile)
        {
            return new Vector2(
                (TextureCoord.X * TileUStep) + ColumnU(Tile.X),
                (TextureCoord.Y * TileVStep) + RowV(Tile.Y));
        }

        public Vector4 GetTileBounds(Point Tile)
        {
            return new Vector4(ColumnU(Tile.X) + BoundsFudge, RowV(Tile.Y) + BoundsFudge, ColumnU(Tile.X + 1) - BoundsFudge, RowV(Tile.Y + 1) - BoundsFudge);
        }
    }
}
