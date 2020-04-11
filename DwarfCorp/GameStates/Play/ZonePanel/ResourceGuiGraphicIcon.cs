using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class ResourceGuiGraphicIcon : Widget
    {
        public TileReference Hilite = null;
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;

        private ResourceType.GuiGraphic _Resource = null;
        public ResourceType.GuiGraphic Resource
        {
            set
            {
                if (!Object.ReferenceEquals(_Resource, value))
                {
                    _Resource = value;
                    if (CachedDynamicSheet != null)
                        CachedDynamicSheet.Discard();
                    CachedDynamicSheet = null;
                    Invalidate();
                }
            }

            get
            {
                return _Resource;
            }
        }

        public override void Construct()
        {
            Font = "font10-outline-numsonly";
            TextHorizontalAlign = HorizontalAlign.Center;
            TextVerticalAlign = VerticalAlign.Bottom;
            TextColor = new Vector4(1, 1, 1, 1);
            WrapText = false;

            OnUpdate = (sender, time) =>
            {
                if (Resource != null && CachedDynamicSheet == null)
                {
                    CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Resource);
                    Invalidate();
                }
            };

            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
                CachedDynamicSheet = null;
            };

            Root.RegisterForUpdate(this);

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var s = Text; // Remove any set text so that it is not drawn behind the icon.
            Text = "";
            var r = base.Redraw();

            if (Hilite != null)
                r.QuadPart()
                    .Scale(32, 32)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(Root.GetTileSheet(Hilite.Sheet).TileMatrix(Hilite.Tile));

            if (_Resource != null)
            {
                    if (CachedDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X, Rect.Y)
                            .Colorize(BackgroundColor)
                            .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));

            }

            Text = s; // If we had some text, restore it and draw it above the icon.
            if (!String.IsNullOrEmpty(Text))
                GetTextMeshPart(r);

            return r;
        }
    }
}
