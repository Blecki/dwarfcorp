using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates.Debug
{
    public class TiledCompositeTextureDebugPanel : Gui.Widget
    {
        private Widget TextureView;
        private Widget Info;
        public WorldManager World;
        private Vector4 TextureViewBackgroundColor = new Vector4(0, 0, 0, 1);
        private Vector2 ViewSize = new Vector2(1, 1);
       
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
                    var sheet = new TileSheetDefinition
                    {
                        Type = TileSheetType.TileSheet,
                        TileHeight = 48,
                        TileWidth = 32,
                        Name = "TROLL"
                    };
                    Root.SpriteAtlas.AddDynamicSheet(null, sheet, texture);
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

            Root.RegisterForPostdraw(this);
        }

        public override void PostDraw(GraphicsDevice device)
        {
            if (Parent.Parent is Gui.Widgets.TabPanel tabs && tabs.SelectedTab == 2)
            {
                var tiledInstanceGroup = World.Renderer.InstanceRenderer.GetCombinedTiledInstance();
                var tex = tiledInstanceGroup.GetAtlasTexture();
                if (tex != null)
                    this.World.UserInterface.Gui.DrawQuad(new Rectangle(TextureView.Rect.X * Root.RenderData.ScaleRatio, TextureView.Rect.Y * Root.RenderData.ScaleRatio,
                        (int)(ViewSize.X * Root.RenderData.ScaleRatio), (int)(ViewSize.Y * Root.RenderData.ScaleRatio)), tex);
            }
        }

        protected override Mesh Redraw()
        {
            var tiledInstanceGroup = World.Renderer.InstanceRenderer.GetCombinedTiledInstance();
            var tex = tiledInstanceGroup.GetAtlasTexture();
            if (tex == null)
                return Mesh.EmptyMesh();

            Info.Text = String.Format("{0} x {1}", tex.Width, tex.Height);

            var textureSize = tex.Bounds;
            ViewSize = new Vector2(tex.Width / Root.RenderData.ScaleRatio, tex.Height / Root.RenderData.ScaleRatio);

            if (ViewSize.X != TextureView.Rect.Width)
            {
                ViewSize.Y = ViewSize.Y * (TextureView.Rect.Width / ViewSize.X);
                ViewSize.X = TextureView.Rect.Width;
            }

            if (ViewSize.Y > TextureView.Rect.Height)
            {
                ViewSize.X = ViewSize.X * (TextureView.Rect.Height / ViewSize.Y);
                ViewSize.Y = TextureView.Rect.Height;
            }

            var basicTiles = Root.GetTileSheet("basic");

            var mesh = Mesh.EmptyMesh();

            mesh.QuadPart()
                    .Texture(basicTiles.TileMatrix(1))
                    .Scale(TextureView.Rect.Width, TextureView.Rect.Height)
                    .Translate(TextureView.Rect.X, TextureView.Rect.Y)
                    .Colorize(TextureViewBackgroundColor);

            return mesh;
        }
    }
}
