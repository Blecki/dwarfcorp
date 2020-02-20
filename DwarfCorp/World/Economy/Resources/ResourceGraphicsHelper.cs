﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class ResourceGraphicsHelper
    {
        public static Texture2D GetResourceTexture(GraphicsDevice Device, ResourceType.GuiGraphic Graphic, String Palette)
        {
            Texture2D r = null;
            var rawAsset = AssetManager.GetContentTexture(Graphic.AssetPath);
            if (Palette != "None" && DwarfSprites.LayerLibrary.FindPalette(Palette).HasValue(out var palette))
                r = TextureTool.CropAndColorSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame, DwarfSprites.LayerLibrary.BasePalette.CachedPalette, palette.CachedPalette);
            else
                r = TextureTool.CropSprite(Device, rawAsset, Graphic.FrameSize, Graphic.Frame);
            return r;
        }

        public static String GetUniqueGraphicsIdentifier(Resource Resource)
        {
            return Resource.TypeName + "&" + Resource.Gui_Graphic.GetSheetIdentifier() + "&" + Resource.Gui_Palette;
        }
    }
}
