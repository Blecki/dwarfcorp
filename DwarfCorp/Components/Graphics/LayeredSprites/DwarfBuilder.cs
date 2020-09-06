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
    // Todo: Move to a dwarf specific folder full of all the stuff to make dwarf entities
    public static class DwarfBuilder
    {
        private static bool __pass(Layer Layer) { return true; }

        private static Layer SelectRandomLayer(CreatureStats Stats, Random Random, String Type, Func<Layer, bool> Filter = null)
        {
            if (Filter == null) Filter = __pass;

            var layers = LayerLibrary.EnumerateLayersOfType(Type).Where(l => Filter(l));
            if (layers.Count() > 0)
                return layers.SelectRandom(Random);
            else
                return LayerLibrary.EnumerateLayersOfType(Type).Where(l => l.Names.Contains("default")).FirstOrDefault();
        }

        private static Layer FindLayerOrDefault(String Type, String Name)
        {
            var r = LayerLibrary.EnumerateLayersOfType(Type).Where(l => l.Names.Contains(Name)).FirstOrDefault();
            if (r == null)
                r = LayerLibrary.EnumerateLayersOfType(Type).Where(l => l.Names.Contains("default")).FirstOrDefault();
            return r;
        }

        private static Layer FindLayer(String Type, String Name)
        {
            return LayerLibrary.EnumerateLayersOfType(Type).Where(l => l.Names.Contains(Name)).FirstOrDefault();
        }

        public class LayerPalettePair
        {
            public Layer Layer;
            public Palette Palette;
        }

        public static List<LayerPalettePair> BuildDwarfLayers(CreatureStats Stats, Random Random)
        {
            var r = new List<LayerPalettePair>();

            foreach (var layerType in LayerLibrary.EnumerateLayerTypes())
                if (layerType.Fundamental)
                {
                    if (r.Any(l => l.Layer.Type == layerType.Name))
                        continue;

                    if (layerType.Gendered && Stats.Gender != Gender.Nonbinary)
                        r.Add(new LayerPalettePair { Layer = SelectRandomLayer(Stats, Random, layerType.Name, l => l.Names.Contains(Stats.Gender.ToString())), Palette = null });
                    else
                        r.Add(new LayerPalettePair { Layer = SelectRandomLayer(Stats, Random, layerType.Name), Palette = null });
                }

            return r;
        }

        public static void AssignPalettes(Dictionary<String, Palette> ChosenPalettes, List<LayerPalettePair> Layers)
        {
            foreach (var layer in Layers)
                if (layer.Palette == null)
                {
                    if (LayerLibrary.GetLayerType(layer.Layer.Type).HasValue(out var layerType) && ChosenPalettes.ContainsKey(layerType.PaletteType))
                        layer.Palette = ChosenPalettes[layerType.PaletteType];
                    else
                        layer.Palette = LayerLibrary.BasePalette;
                }
        }

        public static Dictionary<String, Palette> ChoosePalettes(Random Random)
        {
            var chosenPalettes = new Dictionary<String, Palette>();
            foreach (var paletteType in LayerLibrary.EnumeratePaletteTypes())
                chosenPalettes.Add(paletteType.Name, LayerLibrary.EnumeratePalettes().Where(p => p.Type == paletteType.Name).SelectRandom(Random));
            return chosenPalettes;
        }

        public static DwarfCharacterSprite CreateDwarfCharacterSprite(ComponentManager Manager, CreatureStats Stats)
        {
            var sprite = new DwarfSprites.DwarfCharacterSprite(Manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)));

            var random = new Random(Stats.RandomSeed);

            var chosenPalettes = ChoosePalettes(random);
            var layers = BuildDwarfLayers(Stats, random);
            AssignPalettes(chosenPalettes, layers);

            foreach (var layer in layers)
                sprite.AddLayer(layer.Layer, layer.Palette);

            sprite.SetAnimations(Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations));
            sprite.SetFlag(GameComponent.Flag.ShouldSerialize, false);
            return sprite;
        }

        public static LayerStack CreateDwarfLayerStack(CreatureStats Stats, MaybeNull<Loadout> Loadout)
        {
            var sprite = new LayerStack();

            var random = new Random(Stats.RandomSeed);

            var chosenPalettes = ChoosePalettes(random);
            var layers = BuildDwarfLayers(Stats, random);
            AssignPalettes(chosenPalettes, layers);

            foreach (var layer in layers)
                sprite.AddLayer(layer.Layer, layer.Palette);

            if (Loadout.HasValue(out var loadout))
                foreach (var item in loadout.StartingEquipment)
                    if (!String.IsNullOrEmpty(item.Equipment_LayerName))
                        if (LayerLibrary.FindLayerWithName(item.Equipment_LayerType, item.Equipment_LayerName).HasValue(out var layer))
                        {
                            if (LayerLibrary.FindPalette(item.Equipment_Palette).HasValue(out var palette))
                                sprite.AddLayer(layer, palette);
                            else
                                sprite.AddLayer(layer, LayerLibrary.BasePalette);
                        }

            return sprite;
        }
    }
}
