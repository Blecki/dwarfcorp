using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    // Todo: Want to make this not dwarf specific.
    public class LayerLibrary
    {
        private static List<PaletteType> PaletteTypes;
        private static List<Palette> Palettes;
        private static Palette _BasePalette = null;
        private static bool PalettesInitialized = false;

        public static List<LayerType> LayerTypes;
        private static List<Layer> Layers;
        private static bool LayersInitialized = false;

        public static Palette BasePalette
        {
            get
            {
                InitializePalettes();
                return _BasePalette;
            }
        }

        #region Palettes

        private static void InitializePalettes()
        {
            if (PalettesInitialized) return;
            PalettesInitialized = true;

            PaletteTypes = FileUtils.LoadJsonListFromMultipleSources<PaletteType>("Entities/Dwarf/Layers/palette-types.json", null, p => p.Name);
            Palettes = FileUtils.LoadJsonListFromMultipleSources<Palette>("Entities/Dwarf/Layers/palettes.json", null, p => p.Name);

            foreach (var palette in Palettes)
            {
                var asset = AssetManager.GetContentTexture(palette.Asset);
                palette.CachedPalette = new DwarfCorp.Palette(TextureTool.RawPaletteFromMemoryTextureRow(TextureTool.MemoryTextureFromTexture2D(asset), palette.Row));
            }

            if (Palettes.Count == 0)
                throw new InvalidProgramException("No palettes?");
            _BasePalette = Palettes.FirstOrDefault(p => p.Type == "Base");

            if (_BasePalette == null)
                _BasePalette = Palettes[0];
        }

        public static IEnumerable<Palette> EnumeratePalettes()
        {
            InitializePalettes();
            return Palettes;
        }

        public static IEnumerable<PaletteType> EnumeratePaletteTypes()
        {
            InitializePalettes();
            return PaletteTypes;
        }

        public static MaybeNull<Palette> FindPalette(String Name)
        {
            return Palettes.FirstOrDefault(p => p.Name == Name);
        }

        #endregion

        #region Layers

        private static void InitializeLayers()
        {
            if (LayersInitialized) return;
            LayersInitialized = true;

            LayerTypes = FileUtils.LoadJsonListFromMultipleSources<LayerType>("Entities/Dwarf/Layers/layer-types.json", null, p => p.Name);
            Layers = new List<Layer>();

            foreach (var file in AssetManager.EnumerateAllFiles("Entities/Dwarf/Layers").Where(filename => System.IO.Path.GetExtension(filename) == ".psd" && filename.Contains("layer")))
                foreach (var sheet in TextureTool.LoadPSD(System.IO.File.OpenRead(file)))
                {
                    var tags = sheet.LayerName.Split(' ');
                    if (tags.Length < 2) continue;

                    var l = new Layer()
                    {
                        CachedTexture = TextureTool.DecomposeTexture(sheet.Data, BasePalette.CachedPalette),
                        Type = tags[0]
                    };

                    l.Names.AddRange(tags.Skip(1));
                    Layers.Add(l);
                }
        }

        public static IEnumerable<Layer> EnumerateLayersOfType(String LayerType)
        {
            InitializeLayers();
            return Layers.Where(l => l.Type == LayerType);
        }

        public static IEnumerable<LayerType> EnumerateLayerTypes()
        {
            InitializeLayers();
            return LayerTypes;
        }

        public static MaybeNull<LayerType> GetLayerType(String Name)
        {
            InitializeLayers();
            return LayerTypes.FirstOrDefault(l => l.Name == Name);
        }

        public static MaybeNull<Layer> FindLayerWithName(String Type, String Name)
        {
            return EnumerateLayersOfType(Type).Where(l => l.Names.Contains(Name)).FirstOrDefault();
        }

        #endregion
    }
}
