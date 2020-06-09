using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class CommandIcon : Widget
    {
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;
        private Gui.TextureAtlas.SpriteAtlasEntry CachedOperationIconDynamicSheet = null;
        public PossiblePlayerCommand Resource;

        public override void Construct()
        {
            Tooltip = Resource.Tooltip;
            MinimumSize = new Point(32, 64);
            Border = "border-thin";

            if (Resource != null && Resource.Icon != null && CachedDynamicSheet == null)
                CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Resource.Icon);
            if (Resource != null && Resource.OperationIcon != null && CachedOperationIconDynamicSheet == null)
                CachedOperationIconDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Resource.OperationIcon);

            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
                if (CachedOperationIconDynamicSheet != null)
                    CachedOperationIconDynamicSheet.Discard();
                CachedDynamicSheet = null;
                CachedOperationIconDynamicSheet = null;
            };

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();

            if (Resource != null)
            {
                if (Resource.OperationIcon != null)
                {
                    if (CachedOperationIconDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + ((Rect.Height / 2) - 32) / 2)
                            .Colorize(BackgroundColor)
                            .Texture(CachedOperationIconDynamicSheet.TileSheet.TileMatrix(0));
                }

                if (Resource.Icon != null)
                {
                    if (CachedDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + 32 + ((Rect.Height / 2) - 32) / 2)
                            .Colorize(BackgroundColor)
                            .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));
                }
                else
                {
                    var tile = Root.GetTileSheet(Resource.OldStyleIcon.Sheet);
                    r.QuadPart()
                        .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + 32 + ((Rect.Height / 2) - 32) / 2)
                        .Colorize(BackgroundColor)
                        .Texture(Root.GetTileSheet(Resource.OldStyleIcon.Sheet).TileMatrix(Resource.OldStyleIcon.Tile));
                }
            }

            return r;
        }
    }
}
