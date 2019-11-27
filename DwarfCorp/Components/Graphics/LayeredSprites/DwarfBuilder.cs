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
    public static class DwarfBuilder
    {
        private static bool __pass(Layer Layer) { return true; }

        private static Layer SelectRandomLayer(CreatureStats Stats, Random Random, LayerType Layer, Func<Layer, bool> Filter = null)
        {
            if (Filter == null) Filter = __pass;

            var layers = LayerLibrary.EnumerateLayers(Layer).Where(l => Filter(l));
            if (layers.Count() > 0)
                return layers.SelectRandom(Random);
            else
                return LayerLibrary.EnumerateLayers(Layer).Where(l => l.Names.Contains("default")).FirstOrDefault();
        }

        private static Layer FindLayerOrDefault(LayerType Layer, String Name)
        {
            var r = LayerLibrary.EnumerateLayers(Layer).Where(l => l.Names.Contains(Name)).FirstOrDefault();
            if (r == null)
                r = LayerLibrary.EnumerateLayers(Layer).Where(l => l.Names.Contains("default")).FirstOrDefault();
            return r;
        }

        public static Dictionary<LayerType, Layer> BuildDwarfLayers(CreatureStats Stats, Random Random)
        {
            var r = new Dictionary<LayerType, Layer>();

            r.Add(LayerType.Body, FindLayerOrDefault(LayerType.Body, Stats.CurrentClass.BodyLayer));

            switch (Stats.Gender)
            {
                case Gender.Female:
                    r.Add(LayerType.Face, FindLayerOrDefault(LayerType.Face, "female"));
                    break;
                case Gender.Male:
                    r.Add(LayerType.Face, FindLayerOrDefault(LayerType.Face, "male"));
                    break;
                default:
                    r.Add(LayerType.Face, SelectRandomLayer(Stats, Random, LayerType.Face));
                    break;
            }

            r.Add(LayerType.Nose, SelectRandomLayer(Stats, Random, LayerType.Nose));
            r.Add(LayerType.Beard, SelectRandomLayer(Stats, Random, LayerType.Beard));

            if (!String.IsNullOrEmpty(Stats.CurrentClass.HatLayer))
                r.Add(LayerType.Hair, FindLayerOrDefault(LayerType.Hair, Stats.CurrentClass.HatLayer));
            else
                r.Add(LayerType.Hair, SelectRandomLayer(Stats, Random, LayerType.Hair, l => !l.Names.Contains("hat")));

            return r;
        }

        public static LayeredCharacterSprite CreateDwarfCharacterSprite(ComponentManager Manager, CreatureStats Stats)
        {
            var sprite = new LayeredSprites.LayeredCharacterSprite(Manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)));

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer == PaletteType.Hair).SelectRandom(random);
            var skinPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer == PaletteType.Skin).SelectRandom(random);

            var layers = BuildDwarfLayers(Stats, random);
            if (layers[LayerType.Body] != null) sprite.AddLayer(layers[LayerType.Body], skinPalette);
            if (layers[LayerType.Face] != null) sprite.AddLayer(layers[LayerType.Face], skinPalette);
            if (layers[LayerType.Nose] != null) sprite.AddLayer(layers[LayerType.Nose], skinPalette);
            if (layers[LayerType.Beard] != null) sprite.AddLayer(layers[LayerType.Beard], hairPalette);
            if (layers[LayerType.Hair] != null) sprite.AddLayer(layers[LayerType.Hair], hairPalette);

            sprite.SetAnimations(Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations));

            sprite.SetFlag(GameComponent.Flag.ShouldSerialize, false);

            return sprite;
        }

        public static LayerStack CreateDwarfLayerStack(CreatureStats Stats)
        {
            var sprite = new LayerStack();

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer == PaletteType.Hair).SelectRandom(random);
            var skinPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer == PaletteType.Skin).SelectRandom(random);

            var layers = BuildDwarfLayers(Stats, random);
            if (layers[LayerType.Body] != null) sprite.AddLayer(layers[LayerType.Body], skinPalette);
            if (layers[LayerType.Face] != null) sprite.AddLayer(layers[LayerType.Face], skinPalette);
            if (layers[LayerType.Nose] != null) sprite.AddLayer(layers[LayerType.Nose], skinPalette);
            if (layers[LayerType.Beard] != null) sprite.AddLayer(layers[LayerType.Beard], hairPalette);
            if (layers[LayerType.Hair] != null) sprite.AddLayer(layers[LayerType.Hair], hairPalette);

            return sprite;
        }

    }
}
