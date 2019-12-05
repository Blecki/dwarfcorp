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
            InitializePalettes();
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

        public static void ProcessImage(MemoryTexture MemTex, String Filename, GraphicsDevice Device)
        {
            var palette = TextureTool.OptimizedPaletteFromMemoryTexture(MemTex);
            palette.Sort((a, b) => (a.R + a.G + a.B) - (b.R + b.G + b.B));

            for (var r = 0; r < MemTex.Height; ++r)
                for (var c = 0; c < MemTex.Width; ++c)
                {
                    var index = MemTex.Index(c, r);
                    var color = MemTex.Data[index];
                    var paletteIndex = palette.IndexOf(color);
                    MemTex.Data[index] = new Color(32 * paletteIndex, 32 * paletteIndex, 32 * paletteIndex);
                }

            for (var c = 0; c < palette.Count; ++c)
                MemTex.Data[MemTex.Index(c, 0)] = palette[c];

            TextureTool.Texture2DFromMemoryTexture(Device, MemTex).SaveAsPng(System.IO.File.OpenWrite(Filename), MemTex.Width, MemTex.Height);
        }

        public static void ConvertTestPSD()
        {
            var stream = System.IO.File.OpenRead("Content/Entities/Dwarf/Layers/test.psd");
            var psd = new PhotoshopFile.PsdFile(stream, new PhotoshopFile.LoadContext());

            if (FindPalette("Hair 02").HasValue(out var conversionPalette))
            {
                foreach (var layer in psd.Layers)
                {
                    var channels = new List<PhotoshopFile.Channel>();
                    channels.Add(layer.Channels.Where(c => c.ID == 0).FirstOrDefault());
                    channels.Add(layer.Channels.Where(c => c.ID == 1).FirstOrDefault());
                    channels.Add(layer.Channels.Where(c => c.ID == 2).FirstOrDefault());
                    channels.Add(layer.AlphaChannel);

                    var rawMemText = new MemoryTexture(layer.Rect.Width, layer.Rect.Height);

                    for (var index = 0; index < layer.Rect.Width * layer.Rect.Height; ++index)
                        if (channels[3].ImageData[index] == 0)
                            rawMemText.Data[index] = new Color(0, 0, 0, 0);
                        else
                            rawMemText.Data[index] = new Color(channels[0].ImageData[index], channels[1].ImageData[index], channels[2].ImageData[index], channels[3].ImageData[index]);

                    var memTex = new MemoryTexture(psd.ColumnCount, psd.RowCount);
                    TextureTool.Blit(rawMemText, new Rectangle(0, 0, layer.Rect.Width, layer.Rect.Height), memTex, new Point(layer.Rect.X, layer.Rect.Y));

                    var decomposed = TextureTool.DecomposeTexture(memTex, conversionPalette.CachedPalette);
                    var composed = TextureTool.ComposeTexture(decomposed, BasePalette.CachedPalette);

                    TextureTool.Blit(composed, new Rectangle(layer.Rect.X, layer.Rect.Y, layer.Rect.Width, layer.Rect.Height), rawMemText, new Rectangle(0, 0, rawMemText.Width, rawMemText.Height));

                    for (var index = 0; index < rawMemText.Width * rawMemText.Height; ++index)
                    {
                        channels[0].ImageData[index] = rawMemText.Data[index].R;
                        channels[1].ImageData[index] = rawMemText.Data[index].G;
                        channels[2].ImageData[index] = rawMemText.Data[index].B;
                        channels[3].ImageData[index] = rawMemText.Data[index].A;
                    }

                    foreach (var channel in layer.Channels)
                        channel.ImageDataRaw = null;
                }
            }

            psd.PrepareSave();
            psd.Save("processed.psd", Encoding.Unicode);
        }

    }
}
