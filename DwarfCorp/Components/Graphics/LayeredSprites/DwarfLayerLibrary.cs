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

namespace DwarfCorp.DwarfSprites
{
    // Todo: Want to make this not dwarf specific.
    public class LayerLibrary
    {
        private static List<Palette> Palettes;
        private static Palette _BaseDwarfPalette = null;
        private static bool PalettesInitialized = false;

        private static List<Layer> Layers;
        private static bool LayersInitialized = false;

        public static Palette BaseDwarfPalette
        {
            get
            {
                InitializePalettes();
                return _BaseDwarfPalette;
            }
        }

        #region Palettes

        private static void InitializePalettes()
        {
            if (PalettesInitialized) return;
            PalettesInitialized = true;

            Palettes = new List<Palette>();
            foreach (var file in AssetManager.EnumerateAllFiles("Entities/Dwarf/Layers").Where(filename => System.IO.Path.GetExtension(filename) == ".psd" && filename.Contains("palette")))
                foreach (var sheet in TextureTool.LoadPSD(System.IO.File.OpenRead(file)))
                    for (var r = 0; r < sheet.Data.Height; ++r)
                        Palettes.Add(new Palette
                        {
                            CachedPalette = new DwarfCorp.Palette(TextureTool.RawPaletteFromMemoryTextureRow(sheet.Data, r).Skip(1)),
                            Asset = GetPaletteTypeFromColor(sheet.Data.Data[sheet.Data.Index(0, r)]).ToString() + " " + r.ToString(),
                            Layer = GetPaletteTypeFromColor(sheet.Data.Data[sheet.Data.Index(0, r)])
                        });

            Palettes.RemoveAll(p => p.Layer == PaletteType.Discard);
            if (Palettes.Count == 0)
                throw new InvalidProgramException("No palettes?");
            _BaseDwarfPalette = Palettes.FirstOrDefault(p => p.Layer == PaletteType.Base);

            if (_BaseDwarfPalette == null)
                _BaseDwarfPalette = Palettes[0];
        }

        private static PaletteType GetPaletteTypeFromColor(Color C)
        {
            if (C == new Color(0, 0, 0, 255))
                return PaletteType.Base;
            if (C == new Color(255, 0, 0, 255))
                return PaletteType.Skin;
            if (C == new Color(0, 255, 0, 255))
                return PaletteType.Hair;
            return PaletteType.Discard;
        }

        public static void SaveCombinedPalette(GraphicsDevice Device)
        {
            var composedPalette = new MemoryTexture(Palettes[0].CachedPalette.Count + 1, Palettes.Count);
            for (var r = 0; r < Palettes.Count; ++r)
            {
                if (Palettes[r].Layer == PaletteType.Base)
                    composedPalette.Data[composedPalette.Index(0, r)] = new Color(0, 0, 0, 255);
                else if (Palettes[r].Layer == PaletteType.Skin)
                    composedPalette.Data[composedPalette.Index(0, r)] = new Color(255, 0, 0, 255);
                else if (Palettes[r].Layer == PaletteType.Hair)
                    composedPalette.Data[composedPalette.Index(0, r)] = new Color(0, 255, 0, 255);

                for (var c = 0; c < composedPalette.Width && c < Palettes[r].CachedPalette.Count; ++c)
                    composedPalette.Data[composedPalette.Index(c + 1, r)] = Palettes[r].CachedPalette[c];
            }
            var realTex = TextureTool.Texture2DFromMemoryTexture(Device, composedPalette);
            realTex.SaveAsPng(System.IO.File.OpenWrite("combined-palette.png"), composedPalette.Width, composedPalette.Height);
        }

        public static IEnumerable<Palette> EnumeratePalettes()
        {
            InitializePalettes();
            return Palettes;
        }

        #endregion

        #region Layers

        private static void InitializeLayers()
        {
            if (LayersInitialized) return;
            LayersInitialized = true;

            Layers = new List<Layer>();

            foreach (var file in AssetManager.EnumerateAllFiles("Entities/Dwarf/Layers").Where(filename => System.IO.Path.GetExtension(filename) == ".psd" && filename.Contains("layer")))
                foreach (var sheet in TextureTool.LoadPSD(System.IO.File.OpenRead(file)))
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

        public static IEnumerable<Layer> EnumerateLayers(LayerType LayerType)
        {
            InitializeLayers();
            return Layers.Where(l => l.Type == LayerType);
        }

        #endregion
    }
}
