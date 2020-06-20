using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization;
using DwarfCorp.Gui;

namespace DwarfCorp
{
    public class ResourceGraphicsHelper
    {
        private static MemoryTexture _GetResourceTexture(GraphicsDevice Device, ResourceType.GuiGraphic Graphic)
        {
            MemoryTexture r = null;
            var rawAsset = AssetManager.GetContentTexture(Graphic.AssetPath);
            if (Graphic.Palette != "None" && DwarfSprites.LayerLibrary.FindPalette(Graphic.Palette).HasValue(out var palette))
                r = TextureTool.CropAndColorSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame, DwarfSprites.LayerLibrary.BasePalette.CachedPalette, palette.CachedPalette);
            else
                r = TextureTool.CropSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame);

            if (Graphic.NextLayer != null)
                TextureTool.AlphaBlit(_GetResourceTexture(Device, Graphic.NextLayer), new Rectangle(0, 0, Graphic.NextLayer.FrameSize.X, Graphic.NextLayer.FrameSize.Y),
                    r, new Point(0, 0));
            return r;
        }

        public static Texture2D GetResourceTexture(GraphicsDevice Device, ResourceType.GuiGraphic Graphic)
        {
            return TextureTool.Texture2DFromMemoryTexture(Device, _GetResourceTexture(Device, Graphic));
        }

        public static Gui.TextureAtlas.SpriteAtlasEntry GetDynamicSheet(Gui.Root Root, Resource Resource)
        {
            return GetDynamicSheet(Root, Resource.Gui_Graphic);
        }

        public static Gui.TextureAtlas.SpriteAtlasEntry GetDynamicSheet(Gui.Root Root, ResourceType.GuiGraphic Graphic)
        {
            if (Graphic == null)
            {
                var tex = AssetManager.GetContentTexture("newgui/error");
                return Root.SpriteAtlas.AddDynamicSheet("error", new TileSheetDefinition
                {
                    TileHeight = 32,
                    TileWidth = 32,
                    Type = TileSheetType.TileSheet
                }, tex);
            }
            else
            {
                var sheetName = Graphic.GetSheetIdentifier();
                var tex = ResourceGraphicsHelper.GetResourceTexture(Root.RenderData.Device, Graphic);
                return Root.SpriteAtlas.AddDynamicSheet(sheetName, new TileSheetDefinition
                {
                    TileHeight = Graphic.FrameSize.Y,
                    TileWidth = Graphic.FrameSize.X,
                    Type = TileSheetType.TileSheet
                }, tex);
            }
        }
    }
}
