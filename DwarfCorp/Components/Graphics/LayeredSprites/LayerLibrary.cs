using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.LayeredSprites
{
    public class LayerLibrary
    {
        private static List<Layer> Layers;
        private static List<Palette> Palettes;
        private static Palette _BaseDwarfPalette = null;

        public enum LayerType
        {
            BODY = 0,
            FACE = 1,
            NOSE = 2,
            BEARD = 3,
            HAIR = 4,
            TOOL = 5
        }

        public static void Cleanup()
        {
            Layers = null;
            Palettes = null;
            _BaseDwarfPalette = null;
        }

        public static Palette BaseDwarfPalette
        {
            get
            {
                if (_BaseDwarfPalette == null)
                {
                    var rawTexture = AssetManager.GetContentTexture(ContentPaths.dwarf_base_palette);
                    _BaseDwarfPalette = new Palette
                    {
                        CachedPalette = TextureTool.RawPaletteFromTexture2D(rawTexture),
                        Asset = ContentPaths.dwarf_base_palette
                    };
                }
                return _BaseDwarfPalette;
            }
        }

        private static void Initialize()
        {
            if (Layers != null && Palettes != null) return;

            var layerFiles = AssetManager.EnumerateAllFiles("Entities/Dwarf/Layers").Where(filename => System.IO.Path.GetExtension(filename) == ".psd");

            Layers = new List<Layer>();

            foreach (var file in layerFiles)
            {
                var psd = TextureTool.LoadPSD(System.IO.File.OpenRead(file));
                foreach (var sheet in psd)
                {
                    var tags = sheet.LayerName.Split(' ');
                    if (tags.Length < 2) continue;
                    var type = tags[0].ToUpper();
                    var enumType = (LayerType)Enum.Parse(typeof(LayerType), type);
                    var l = new Layer();
                    l.CachedTexture = TextureTool.DecomposeTexture(sheet.Data, BaseDwarfPalette.CachedPalette);
                    l.Type = enumType;
                    l.Names.AddRange(tags.Skip(1));
                    Layers.Add(l);
                }
            }

            //Layers = FileUtils.LoadJsonListFromMultipleSources<Layer>(ContentPaths.dwarf_layers, null, l => l.Type + "&" + l.Asset);

            //foreach (var layer in Layers)
            //{
            //    var rawTexture = AssetManager.GetContentTexture(layer.Asset);
            //    var memTexture = TextureTool.MemoryTextureFromTexture2D(rawTexture);
            //    if (memTexture == null)
            //        continue;
            //    layer.CachedTexture = TextureTool.DecomposeTexture(memTexture, BaseDwarfPalette.CachedPalette);
            //}

            Palettes = FileUtils.LoadJsonListFromMultipleSources<Palette>(ContentPaths.dwarf_palettes, null, l => l.Asset);

            foreach (var palette in Palettes)
            {
                var rawTexture = AssetManager.GetContentTexture(palette.Asset);
                palette.CachedPalette = TextureTool.RawPaletteFromTexture2D(rawTexture);
            }
        }

        public static IEnumerable<Layer> EnumerateLayers(LayerType LayerType)
        {
            Initialize();

            if (Palettes == null)
                throw new Exception("Dwarf Layers failed to load.");

            return Layers.Where(l => l.Type == LayerType);
        }

        public static IEnumerable<Palette> EnumeratePalettes()
        {
            Initialize();

            if (Palettes == null)
                throw new Exception("Dwarf Layers failed to load.");

            return Palettes;
        }
    }
}
