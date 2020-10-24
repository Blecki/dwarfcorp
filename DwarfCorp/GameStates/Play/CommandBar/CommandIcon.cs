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
        public PossiblePlayerCommand Command;
        public Gui.Widget MenuBarIcon;

        public override void Construct()
        {
            Tooltip = Command.Tooltip;
            MinimumSize = new Point(32, 64);
            Border = "border-thin";
            Background = new TileReference("basic", 1);
            BackgroundColor = new Vector4(0, 0, 0, 1.0f);

            if (Command != null && Command.Icon != null && CachedDynamicSheet == null)
            {
                CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Command.Icon);
                MenuBarIcon = new Gui.Widgets.FlatToolTray.Icon
                {
                    NewStyleIcon = Command.Icon
                };
            }

            if (Command != null && Command.OperationIcon != null && CachedOperationIconDynamicSheet == null)
            {
                CachedOperationIconDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Command.OperationIcon);
                if (MenuBarIcon == null)
                    MenuBarIcon = new Gui.Widgets.FlatToolTray.Icon
                    {
                        NewStyleIcon = Command.OperationIcon
                    };
            }

            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
                if (CachedOperationIconDynamicSheet != null)
                    CachedOperationIconDynamicSheet.Discard();
                CachedDynamicSheet = null;
                CachedOperationIconDynamicSheet = null;
            };

            if (MenuBarIcon != null)
                Root.ConstructWidget(MenuBarIcon);

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();

            if (Command != null)
            {
                if (Command.OperationIcon != null)
                {
                    if (CachedOperationIconDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + ((Rect.Height / 2) - 32) / 2)
                            .Texture(CachedOperationIconDynamicSheet.TileSheet.TileMatrix(0));
                }

                if (Command.Icon != null)
                {
                    if (CachedDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + 32 + ((Rect.Height / 2) - 32) / 2)
                            .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));
                }
                else
                {
                    var tile = Root.GetTileSheet(Command.OldStyleIcon.Sheet);
                    r.QuadPart()
                        .Scale(32, 32)
                            .Translate(Rect.X + (Rect.Width - 32) / 2, Rect.Y + 32 + ((Rect.Height / 2) - 32) / 2)
                        .Texture(Root.GetTileSheet(Command.OldStyleIcon.Sheet).TileMatrix(Command.OldStyleIcon.Tile));
                }
            }

            return r;
        }
    }
}
