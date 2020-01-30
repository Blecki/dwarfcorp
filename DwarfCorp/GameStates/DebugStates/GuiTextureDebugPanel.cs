using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.GameStates.Debug
{
    public class GuiTextureDebugPanel : Gui.Widget
    {
        private Widget TextureView;
        private Widget Info;
        private Vector4 TextureViewBackgroundColor = new Vector4(0, 0, 0, 1);

        public override void Construct()
        {
            var bar = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32),
                Background = new TileReference("basic", 1)
            });

            bar.AddChild(new Widget
            {
                Text = "BLACK",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
                OnClick = (sender, args) =>
                {
                    TextureViewBackgroundColor = new Vector4(0, 0, 0, 1);
                    Invalidate();
                }
            });

            bar.AddChild(new Widget
            {
                Text = "WHITE",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
                OnClick = (sender, args) =>
                {
                    TextureViewBackgroundColor = new Vector4(1, 1, 1, 1);
                    Invalidate();
                }
            });

            bar.AddChild(new Widget
            {
                Text = "!!!!",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
                OnClick = (sender, args) =>
                {
                    var texture = AssetManager.GetContentTexture("Entities/Troll/troll");
                    var sheet = new JsonTileSheet
                    {
                        Type = JsonTileSheetType.TileSheet,
                        TileHeight = 48,
                        TileWidth = 32,
                        Name = "TROLL"
                    };
                    Root.RenderData.AddDynamicSheet(sheet, texture);
                    Invalidate();
                }
            });

            Info = bar.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 2,
                TextVerticalAlign = VerticalAlign.Center
            });

            TextureView = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Transparent = true
            });
        }

        protected override Mesh Redraw()
        {
            Info.Text = String.Format("{0} x {1}", Root.RenderData.Texture.Width, Root.RenderData.Texture.Height);

            var textureSize = Root.RenderData.Texture.Bounds;
            var newSize = new Vector2(textureSize.Width, textureSize.Height);

            if (newSize.X != TextureView.Rect.Width)
            {
                newSize.Y = newSize.Y * (TextureView.Rect.Width / newSize.X);
                newSize.X = TextureView.Rect.Width;
            }

            if (newSize.Y > TextureView.Rect.Height)
            {
                newSize.X = newSize.X * (TextureView.Rect.Height / newSize.Y);
                newSize.Y = TextureView.Rect.Height;
            }

            var basicTiles = Root.GetTileSheet("basic");

            var mesh = Mesh.EmptyMesh();

            mesh.QuadPart()
                    .Texture(basicTiles.TileMatrix(1))
                    .Scale(TextureView.Rect.Width, TextureView.Rect.Height)
                    .Translate(TextureView.Rect.X, TextureView.Rect.Y)
                    .Colorize(TextureViewBackgroundColor);

            mesh.QuadPart()
                .Scale(newSize.X, newSize.Y)
                .Translate(TextureView.Rect.Center.X - (newSize.X / 2), TextureView.Rect.Center.Y - (newSize.Y / 2));

            return mesh;
        }
    }
}
