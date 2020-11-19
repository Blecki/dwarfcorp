using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.Gui.Widgets.Minimap
{
    public class MinimapFrame : Window
    {
        public String Frame = "minimap-frame";
        public MinimapRenderer Renderer;
        private TextureAtlas.SpriteAtlasEntry DynamicAtlasEntry = null;

        public override Point GetBestSize()
        {
            return new Point(Renderer.RenderWidth + 20, Renderer.RenderHeight + 20);
        }

        public override void Construct()
        {
            MinimumSize = GetBestSize();
            MaximumSize = GetBestSize();

            this.OnUpdate = (sender, time) =>
            {
                if (Hidden)
                    return;

                if (IsAnyParentHidden())
                    return;

                Renderer.PreRender(DwarfGame.SpriteBatch);

                if (DynamicAtlasEntry == null)
                {
                    var tex = new Texture2D(Root.RenderData.Device, Renderer.RenderWidth, Renderer.RenderHeight);
                    DynamicAtlasEntry = Root.SpriteAtlas.AddDynamicSheet(null,
                        new TileSheetDefinition
                        {
                            TileHeight = Renderer.RenderWidth,
                            TileWidth = Renderer.RenderHeight,
                            RepeatWhenUsedAsBorder = false,
                            Type = TileSheetType.TileSheet
                        },
                        tex);
                }

                if (Renderer.RenderTarget != null)
                    DynamicAtlasEntry.ReplaceTexture(Renderer.RenderTarget);

                this.Invalidate();
            };

            OnClick = (sender, args) =>
                {
                    var localX = args.X - Rect.X;
                    var localY = args.Y - Rect.Y;

                    if (localX < Renderer.RenderWidth && localY > 12)
                        Renderer.OnClicked(localX, localY);
                };


            var buttonRow = AddChild(new Gui.Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 20),
                AutoLayout = Gui.AutoLayout.DockTop,
                Padding = new Gui.Margin(2, 2, 2, 2)
            });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
                {
                    Background = new Gui.TileReference("round-buttons", 0),
                    MinimumSize = new Point(16, 16),
                    MaximumSize = new Point(16, 16),
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    OnClick = (sender, args) => Renderer.ZoomIn(),
                    Tooltip = "Zoom in"
                });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 1),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomOut(),
                Tooltip = "Zoom out"
            });

            buttonRow.AddChild(new Gui.Widgets.ImageButton
            {
                Background = new Gui.TileReference("round-buttons", 2),
                MinimumSize = new Point(16, 16),
                MaximumSize = new Point(16, 16),
                AutoLayout = Gui.AutoLayout.DockLeft,
                OnClick = (sender, args) => Renderer.ZoomHome(),
                Tooltip = "Zoom to home base"
            });

            OnScroll = (sender, args) =>
            {
                float multiplier = GameSettings.Current.InvertZoom ? 0.001f : -0.001f;
                Renderer.Zoom(args.ScrollValue * multiplier);
            };

            Root.RegisterForUpdate(this);
            base.Construct();
        }

        

        protected override Gui.Mesh Redraw()
        {
            var mesh = Mesh.EmptyMesh();
            if (DynamicAtlasEntry != null)
                mesh.QuadPart().Scale(Renderer.RenderWidth, Renderer.RenderHeight).Translate(Rect.X + 10, Rect.Y + 10).Texture(DynamicAtlasEntry.TileSheet.TileMatrix(0));
            mesh.Scale9Part(Rect, Root.GetTileSheet("window-transparent"), Scale9Corners.All);
            AddCloseButtonMesh(mesh);
            return mesh;
        }
    }
}
