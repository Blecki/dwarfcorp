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

        public static List<Layer> BuildDwarfLayers(CreatureStats Stats, Random Random)
        {
            var r = new List<Layer>();

            foreach (var preset in Stats.CurrentClass.PresetLayers)
            {
                var l = FindLayer(preset.LayerType, preset.LayerName);
                if (l != null)
                    r.Add(l);
            }

            foreach (var layerType in LayerLibrary.EnumerateLayerTypes())
                if (layerType.Fundamental)
                {
                    if (r.Any(l => l.Type == layerType.Name))
                        continue;

                    if (layerType.Gendered && Stats.Gender != Gender.Nonbinary)
                        r.Add(SelectRandomLayer(Stats, Random, layerType.Name, l => l.Names.Contains(Stats.Gender.ToString())));
                    else
                        r.Add(SelectRandomLayer(Stats, Random, layerType.Name));
                }

            return r;
        }

        public static LayeredCharacterSprite CreateDwarfCharacterSprite(ComponentManager Manager, CreatureStats Stats)
        {
            var sprite = new DwarfSprites.LayeredCharacterSprite(Manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)));

            var random = new Random(Stats.RandomSeed);

            var chosenPalettes = new Dictionary<String, Palette>();
            foreach (var paletteType in LayerLibrary.EnumeratePaletteTypes())
                chosenPalettes.Add(paletteType.Name, LayerLibrary.EnumeratePalettes().Where(p => p.Type == paletteType.Name).SelectRandom(random));

            var layers = BuildDwarfLayers(Stats, random);
            foreach (var layer in layers)
                if (LayerLibrary.GetLayerType(layer.Type).HasValue(out var layerType))
                {
                    if (chosenPalettes.ContainsKey(layerType.PaletteType))
                        sprite.AddLayer(layer, chosenPalettes[layerType.PaletteType]);
                    else
                        sprite.AddLayer(layer, LayerLibrary.BasePalette);
                }

            sprite.SetAnimations(Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations));
            sprite.SetFlag(GameComponent.Flag.ShouldSerialize, false);
            return sprite;
        }

        public static LayerStack CreateDwarfLayerStack(CreatureStats Stats)
        {
            var sprite = new LayerStack();

            var random = new Random(Stats.RandomSeed);

            var chosenPalettes = new Dictionary<String, Palette>();
            foreach (var paletteType in LayerLibrary.EnumeratePaletteTypes())
                chosenPalettes.Add(paletteType.Name, LayerLibrary.EnumeratePalettes().Where(p => p.Type == paletteType.Name).SelectRandom(random));

            var layers = BuildDwarfLayers(Stats, random);
            foreach (var layer in layers)
                if (LayerLibrary.GetLayerType(layer.Type).HasValue(out var layerType))
                {
                    if (chosenPalettes.ContainsKey(layerType.PaletteType))
                        sprite.AddLayer(layer, chosenPalettes[layerType.PaletteType]);
                    else
                        sprite.AddLayer(layer, LayerLibrary.BasePalette);
                }

            return sprite;
        }
    }
}
