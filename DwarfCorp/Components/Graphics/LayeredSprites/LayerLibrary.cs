using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public enum LayerType
    {
        Body = 0,
        Face = 1,
        Nose = 2,
        Beard = 3,
        Hair = 4,
        Tool = 5
    }
}

namespace DwarfCorp.LayeredSprites
{
    public class LayerLibrary
    {
        private static List<Layer> Layers;
        private static List<Palette> Palettes;
        private static Palette _BaseDwarfPalette = null;

        

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
                    var rawTexture = AssetManager.GetContentTexture("Entities/Dwarf/Layers/base palette");
                    _BaseDwarfPalette = new Palette
                    {
                        CachedPalette = TextureTool.RawPaletteFromTexture2D(rawTexture),
                        Asset = "Entities/Dwarf/Layers/base palette"
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

                    if (!Enum.TryParse<LayerType>(tags[0], true, out var layerType))
                        throw new InvalidOperationException("Malformed dwarf layer - unknown layer type.");

                    var l = new Layer()
                    {
                        CachedTexture = TextureTool.DecomposeTexture(sheet.Data, BaseDwarfPalette.CachedPalette),
                        Type = layerType
                    };

                    l.Names.AddRange(tags.Skip(1));
                    Layers.Add(l);
                }
            }

            Palettes = AssetManager.EnumerateAllFiles("Entities/Dwarf/Layers").Where(filename => filename.Contains("palette")).Select(f =>
            {
                var tags = System.IO.Path.GetFileNameWithoutExtension(f).Split(' ');
                if (!Enum.TryParse<PaletteType>(tags[0], true, out var paletteType))
                    throw new InvalidOperationException("Malformed dwarf palette - unknown palette type when processing " + f);

                return new Palette
                {
                    Asset = f,
                    Layer = paletteType,
                    CachedPalette = TextureTool.RawPaletteFromTexture2D(AssetManager.GetContentTexture(f))
                };
            }).ToList();
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
