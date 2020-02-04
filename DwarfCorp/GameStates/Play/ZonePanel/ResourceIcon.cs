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
        public String Hilite = null;
        private Gui.TextureAtlas.SpriteAtlasEntry CachedDynamicSheet = null;

        private Resource _Resource = null;
        public Resource Resource
        {
            set
            {
                _Resource = value;
                CachedDynamicSheet = null;
                Invalidate();
            }

            get
            {
                return _Resource;
            }
        }
        
        private Gui.TextureAtlas.SpriteAtlasEntry GetDynamicSheet()
        {
            if (Resource.ResourceType.HasValue(out var res))
            {
                var sheetName = Resource.TypeName + "&" + res.Gui_Graphic.GetSheetIdentifier() + "&" + res.Gui_Palette; // Todo - will need to promote to pass through props

                var asset = AssetManager.GetContentTexture(res.Gui_Graphic.AssetPath);
                if (DwarfSprites.LayerLibrary.FindPalette(res.Gui_Palette).HasValue(out var palette))
                {
                    var tex = TextureTool.CropAndColorSprite(Root.RenderData.Device, asset, res.Gui_Graphic.FrameSize, res.Gui_Graphic.Frame,
                        DwarfSprites.LayerLibrary.BasePalette.CachedPalette, palette.CachedPalette);
                    return Root.SpriteAtlas.AddDynamicSheet(sheetName, new TileSheetDefinition
                    {
                        TileHeight = res.Gui_Graphic.FrameSize.Y,
                        TileWidth = res.Gui_Graphic.FrameSize.X,
                        Type = TileSheetType.TileSheet
                    }, tex);
                }
            }

            return null;
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
                if (Resource != null && Resource.ResourceType.HasValue(out var res) && res.Gui_NewStyle && CachedDynamicSheet == null)
                    CachedDynamicSheet = GetDynamicSheet();
            };

            Root.RegisterForUpdate(this);

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();

            if (_Resource == null)
                return r;

            if (OverrideTooltip)
                Tooltip = String.Format("{0}\n{1}\nWear: {2:##.##}%", _Resource.DisplayName, _Resource.Description, (_Resource.Tool_Wear / _Resource.Tool_Durability) * 100.0f);

            if (!String.IsNullOrEmpty(Hilite))
                r.QuadPart()
                    .Scale(32, 32)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(Root.GetTileSheet(Hilite).TileMatrix(0));

            if (Resource.ResourceType.HasValue(out var res) && res.Gui_NewStyle)
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

            return r;
        }
    }
}
