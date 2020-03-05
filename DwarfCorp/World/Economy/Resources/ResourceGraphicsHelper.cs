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
        public static Texture2D GetResourceTexture(GraphicsDevice Device, ResourceType.GuiGraphic Graphic)
        {
            Texture2D r = null;
            var rawAsset = AssetManager.GetContentTexture(Graphic.AssetPath);
            if (Graphic.Palette != "None" && DwarfSprites.LayerLibrary.FindPalette(Graphic.Palette).HasValue(out var palette))
                r = TextureTool.CropAndColorSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame, DwarfSprites.LayerLibrary.BasePalette.CachedPalette, palette.CachedPalette);
            else
                r = TextureTool.CropSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame);
            return r;
        }

        public static Gui.TextureAtlas.SpriteAtlasEntry GetDynamicSheet(Gui.Root Root, Resource Resource)
        {
            return GetDynamicSheet(Root, Resource.Gui_Graphic);
        }

        public static Gui.TextureAtlas.SpriteAtlasEntry GetDynamicSheet(Gui.Root Root, ResourceType.GuiGraphic Graphic)
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
