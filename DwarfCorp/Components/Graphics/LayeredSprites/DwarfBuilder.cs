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

            r.Add(LayerType.BODY, FindLayerOrDefault(LayerType.BODY, Stats.CurrentClass.BodyLayer));

            switch (Stats.Gender)
            {
                case Gender.Female:
                    r.Add(LayerType.FACE, FindLayerOrDefault(LayerType.FACE, "female"));
                    break;
                case Gender.Male:
                    r.Add(LayerType.FACE, FindLayerOrDefault(LayerType.FACE, "male"));
                    break;
                default:
                    r.Add(LayerType.FACE, SelectRandomLayer(Stats, Random, LayerType.FACE));
                    break;
            }

            r.Add(LayerType.NOSE, SelectRandomLayer(Stats, Random, LayerType.NOSE));
            r.Add(LayerType.BEARD, SelectRandomLayer(Stats, Random, LayerType.BEARD));

            if (!String.IsNullOrEmpty(Stats.CurrentClass.HatLayer))
                r.Add(LayerType.HAIR, FindLayerOrDefault(LayerType.HAIR, Stats.CurrentClass.HatLayer));
            else
                r.Add(LayerType.HAIR, SelectRandomLayer(Stats, Random, LayerType.HAIR, l => !l.Names.Contains("hat")));

            return r;
        }

        public static LayeredCharacterSprite CreateDwarfCharacterSprite(ComponentManager Manager, CreatureStats Stats)
        {
            var sprite = new LayeredSprites.LayeredCharacterSprite(Manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)));

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("hair")).SelectRandom(random);
            var skinPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("face")).SelectRandom(random);

            var layers = BuildDwarfLayers(Stats, random);
            if (layers[LayerType.BODY] != null) sprite.AddLayer(layers[LayerType.BODY], skinPalette);
            if (layers[LayerType.FACE] != null) sprite.AddLayer(layers[LayerType.FACE], skinPalette);
            if (layers[LayerType.NOSE] != null) sprite.AddLayer(layers[LayerType.NOSE], skinPalette);
            if (layers[LayerType.BEARD] != null) sprite.AddLayer(layers[LayerType.BEARD], hairPalette);
            if (layers[LayerType.HAIR] != null) sprite.AddLayer(layers[LayerType.HAIR], hairPalette);

            sprite.SetAnimations(Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations));

            sprite.SetFlag(GameComponent.Flag.ShouldSerialize, false);

            return sprite;
        }

        public static LayerStack CreateDwarfLayerStack(CreatureStats Stats)
        {
            var sprite = new LayerStack();

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("hair")).SelectRandom(random);
            var skinPalette = LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("face")).SelectRandom(random);

            var layers = BuildDwarfLayers(Stats, random);
            if (layers[LayerType.BODY] != null) sprite.AddLayer(layers[LayerType.BODY], skinPalette);
            if (layers[LayerType.FACE] != null) sprite.AddLayer(layers[LayerType.FACE], skinPalette);
            if (layers[LayerType.NOSE] != null) sprite.AddLayer(layers[LayerType.NOSE], skinPalette);
            if (layers[LayerType.BEARD] != null) sprite.AddLayer(layers[LayerType.BEARD], hairPalette);
            if (layers[LayerType.HAIR] != null) sprite.AddLayer(layers[LayerType.HAIR], hairPalette);

            return sprite;
        }

    }
}
