using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SpriteSheet
    {
        protected bool Equals(SpriteSheet other)
        {
            return FrameWidth == other.FrameWidth && FrameHeight == other.FrameHeight && string.Equals(AssetName, other.AssetName);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = FrameWidth;
                hashCode = (hashCode*397) ^ FrameHeight;
                hashCode = (hashCode*397) ^ (AssetName != null ? AssetName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        private Texture2D FixedTexture { get; set; }
        public string AssetName { get; set; }

        public void SwapFixedTexture(Texture2D Texture)
        {
            this.FixedTexture = Texture; // Better be same size!
        }

        public SpriteSheet()
        {
            FrameWidth = -1;
            FrameHeight = -1;
            AssetName = "";
        }

        public SpriteSheet(Texture2D texture)
        {
            FixedTexture = texture;
        }

        public SpriteSheet(Texture2D texture, int frameWidth, int frameHeight)
        {
            FixedTexture = texture;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            Width = texture.Width;
            Height = texture.Height;
        }

        public SpriteSheet(string name)
        {
            AssetName = name;
            FixedTexture = AssetManager.GetContentTexture(name);

            if (FixedTexture != null)
            {
                FrameWidth = FixedTexture.Width;
                FrameHeight = FixedTexture.Height;
                Width = FixedTexture.Width;
                Height = FixedTexture.Height;
            }
        }

        public SpriteSheet(string name, int frameWidth, int frameHeight)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            AssetName = name;
            FixedTexture = AssetManager.GetContentTexture(name);
            if (FixedTexture != null)
            {
                Width = FixedTexture.Width;
                Height = FixedTexture.Height;
            }
            else
            {
                Width = frameWidth;
                Height = frameHeight;
            }
        }

        public SpriteSheet(string name, int frameSize)
        {
            FrameWidth = frameSize;
            FrameHeight = frameSize;
            AssetName = name;
            FixedTexture = AssetManager.GetContentTexture(name);
            if (FixedTexture != null)
            {
                Width = FixedTexture.Width;
                Height = FixedTexture.Height;
            }
            else
            {
                Width = frameSize;
                Height = frameSize;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpriteSheet) obj);
        }

        public List<NamedImageFrame> GenerateFrames()
        {
            List<NamedImageFrame> toReturn = new List<NamedImageFrame>();
            Texture2D texture = AssetManager.GetContentTexture(AssetName);

            if (texture == null) return null;

            for (int r = 0; r < texture.Height/FrameHeight; r++)
            {
                for (int c = 0; c < texture.Height/FrameHeight; c++)
                {
                    toReturn.Add(new NamedImageFrame(AssetName, FrameWidth, c, r));
                }
            }

            return toReturn;
        }

        public Texture2D GetTexture()
        {
            if (FixedTexture == null || FixedTexture.IsDisposed || FixedTexture.GraphicsDevice.IsDisposed)
            {
                FixedTexture = AssetManager.GetContentTexture(AssetName);
            }
            return FixedTexture;
        }

        public NamedImageFrame GenerateFrame(Point position)
        {
            return new NamedImageFrame(AssetName, new Rectangle(position.X * FrameWidth, position.Y * FrameHeight, FrameWidth, FrameHeight));
        }

        public Vector2[] GenerateTileUVs(Point Frame, out Vector4 Bounds)
        {
            float normalizeX = FrameWidth / (float)(Width);
            float normalizeY = FrameHeight / (float)(Height);

            var uvs = new Vector2[]
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(0.0f, 1.0f)
                };

            Vector2 pixelCoords = new Vector2(Frame.X * FrameWidth, Frame.Y * FrameHeight);
            Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float)Width, pixelCoords.Y / (float)Height);
            Bounds = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

            for (int vert = 0; vert < 4; vert++)
                uvs[vert] = new Vector2(normalizedCoords.X + uvs[vert].X * normalizeX, normalizedCoords.Y + uvs[vert].Y * normalizeY);

            return uvs;
        }

        public int Columns { get { return FrameWidth > 0 ? Width / FrameWidth : 0; } }
        public int Rows { get { return FrameHeight > 0 ? Height / FrameHeight : 0; } }
        public int Row(int TileIndex) { return Columns > 0 ? TileIndex / Columns : 0; }
        public int Column(int TileIndex) { return TileIndex % Columns; }
        public float TileUStep { get { return Columns > 0 ? 1.0f / Columns : 0; } }
        public float TileVStep { get { return Rows > 0 ? 1.0f / Rows : 0; } }
        public float ColumnU(int Column) { return (TileUStep * Column); }
        public float RowV(int Row) { return (TileVStep * Row); }
        public float TileU(int TileIndex) { return ColumnU(Column(TileIndex)); }
        public float TileV(int TileIndex) { return RowV(Row(TileIndex)); }
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

        public Rectangle GetTileRectangle(Point Tile)
        {
            return new Rectangle(Tile.X * FrameWidth, Tile.Y * FrameHeight, FrameWidth, FrameHeight);
        }
    }
}
