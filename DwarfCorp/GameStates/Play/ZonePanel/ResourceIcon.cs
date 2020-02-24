using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Play
{
    public class ResourceIcon : Widget
    {
        public bool OverrideTooltip = true;
        public bool EnableDragAndDrop = false;
        public Func<Widget, DragAndDrop.DraggedItem> CreateDraggableItem = null;
        public TileReference Hilite = null;
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;

        private Resource _Resource = null;
        public Resource Resource
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
            if (EnableDragAndDrop && CreateDraggableItem != null)
                OnMouseDown = (sender, args) =>
                {
                    var draggedItem = CreateDraggableItem(this);
                    if (draggedItem != null)
                        DragAndDrop.BeginDrag(Root, draggedItem);
                };


            Font = "font10-outline-numsonly";
            TextHorizontalAlign = HorizontalAlign.Center;
            TextVerticalAlign = VerticalAlign.Bottom;
            TextColor = new Vector4(1, 1, 1, 1);
            WrapText = false;

            OnUpdate = (sender, time) =>
            {
                if (Resource != null && Resource.Gui_NewStyle && CachedDynamicSheet == null)
                {
                    CachedDynamicSheet = ResourceGraphicsHelper.GetDynamicSheet(Root, Resource);
                    Invalidate();
                }
            };

            OnClose = (sender) =>
            {
                if (CachedDynamicSheet != null)
                    CachedDynamicSheet.Discard();
            };

            Root.RegisterForUpdate(this);

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();

            if (Hilite != null)
                r.QuadPart()
                    .Scale(32, 32)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(Root.GetTileSheet(Hilite.Sheet).TileMatrix(Hilite.Tile));

            if (_Resource != null)
            {
                if (_Resource.Gui_NewStyle)
                {
                    if (CachedDynamicSheet != null)
                        r.QuadPart()
                            .Scale(32, 32)
                            .Translate(Rect.X, Rect.Y)
                            .Colorize(BackgroundColor)
                            .Texture(CachedDynamicSheet.TileSheet.TileMatrix(0));
                }
                else
                {
                    foreach (var layer in _Resource.GuiLayers)
                        r.QuadPart()
                                    .Scale(32, 32)
                                    .Translate(Rect.X, Rect.Y)
                                    .Colorize(BackgroundColor)
                                    .Texture(Root.GetTileSheet(layer.Sheet).TileMatrix(layer.Tile));
                }

                if (OverrideTooltip)
                    Tooltip = String.Format("{0}\n{1}\nWear: {2:##.##}%", _Resource.DisplayName, _Resource.Description, (_Resource.Tool_Wear / _Resource.Tool_Durability) * 100.0f);
            }

            return r;
        }
    }
}
