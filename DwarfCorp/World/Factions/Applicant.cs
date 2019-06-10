using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Applicant
    {
        public CreatureClass Class { get; set; }
        public String ClassName;
        public CreatureClass.Level Level => Class.Levels[LevelIndex];
        public int LevelIndex = 0;
        public string Name { get; set; }
        public string CoverLetter { get; set; }
        public string FormerProfession { get; set; }
        public string HomeTown { get; set; }
        public string Biography { get; set; }
        public Gender Gender { get; set; }
        public int RandomSeed { get; set; }

        public DwarfBux SigningBonus => Level.Pay * 4;

        public Applicant()
        {
            RandomSeed = MathFunctions.Random.Next();
        }

        public static string GenerateBiography(string Name, Gender Gender)
        {
            var hobbyTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.hobby);
            var hobby = TextGenerator.GenerateRandom(new List<string>(),
                    hobbyTemplates[MathFunctions.Random.Next(hobbyTemplates.Count)].ToArray());
            var biographyTemplates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.biography);
            return TextGenerator.ToSentenceCase(TextGenerator.GenerateRandom(
            new List<string> { Name, Gender.ToString(), hobby, Mating.Pronoun(Gender), Mating.Posessive(Gender) },
            biographyTemplates[MathFunctions.Random.Next(biographyTemplates.Count)].ToArray()));

        }

        public static Applicant Random(String ClassName, CompanyInformation info)
        {
            var r = new Applicant();
            r.GenerateRandom(ClassName, 0, info);
            return r;
        }

        public void GenerateRandom(String ClassName, int level, CompanyInformation info)
        {
            this.ClassName = ClassName;
            Class = Library.GetClass(ClassName);
            LevelIndex = level;
            Gender = Mating.RandomGender();
            Name = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            List<string> justifications = new List<string>()
            {
                "I have many relevant qualities!",
                "My expereince is extensive!",
                "I really need this job!",
                "I need to get away from it all!",
                "I will be your loyal servant!",
                "I've always wanted to work at " + info.Name + "!",
                "I am a very hard worker!",
                "I am an adventurous soul!"
            };
           CoverLetter =
                TextGenerator.GenerateRandom("${Dear,Hey,Hi,Hello,Sup,Yo}", " " , info.Name , ",\n    ",
                "${Please,Do}", " ", "${consider,check out,look at,see,view}", " ", "${my,this}", " ",
                "${application for the position of, resume for, request to be a,offer as}", " " 
                + Level.Name + ". " + justifications[MathFunctions.Random.Next(justifications.Count)] +" \n",
                                             "${Thanks,Sincerely,Yours,--,Always}", ",\n    " ,Name);

            if (level > 0)
            {
                FormerProfession = Class.Levels[level - 1].Name;
            }
            else
            {
                FormerProfession = TextGenerator.GenerateRandom("$profession");
            }

            
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.location);
             HomeTown = TextGenerator.GenerateRandom(new List<string>(),
                 templates[MathFunctions.Random.Next(templates.Count)].ToArray());

            Biography = GenerateBiography(Name, Gender);
        }

        public LayeredSprites.LayerStack GetLayers()
        {
            var random = new Random(RandomSeed);

            var hairPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("hair")).SelectRandom(random);
            var skinPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("face")).SelectRandom(random);
            LayeredSprites.LayerStack sprite = new LayeredSprites.LayerStack();

            CreatureStats stats = new CreatureStats("Dwarf", ClassName, LevelIndex)
            {
                Gender = this.Gender
            };
            
            AddLayerOrDefault(sprite, random, "body", stats, skinPalette);
            AddLayerOrDefault(sprite, random, "face", stats, skinPalette);
            AddLayerOrDefault(sprite, random, "nose", stats, skinPalette);
            AddLayerOrDefault(sprite, random, "beard", stats, hairPalette);
            AddLayerOrDefault(sprite, random, "hair", stats, hairPalette);
            AddLayerOrDefault(sprite, random, "tool", stats);
            AddLayerOrDefault(sprite, random, "hat", stats, hairPalette);
            return sprite;
        }

        public AnimationPlayer GetAnimationPlayer(LayeredSprites.LayerStack stack, String Anim = "IdleFORWARD")
        {
            foreach (var animation in Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
                if (animation.Key == Anim)
                    return new AnimationPlayer(stack.ProxyAnimation(animation.Value));
            return null;
        }


        private void AddLayerOrDefault(LayeredSprites.LayerStack stack, Random Random, String Layer, CreatureStats stats, LayeredSprites.Palette Palette = null)
        {
            var layers = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => !l.DefaultLayer && l.PassesFilter(stats));

            if (layers.Count() > 0)
            {
                var newLayer = layers.SelectRandom(Random);
                stack.AddLayer(newLayer, Palette);
                // Do not allow hats and hair on the same head.
                if (newLayer.Asset != "Entities/Dwarf/Layers/blank" && Layer == "hat")
                {
                    stack.RemoveLayer("hair");
                }
            }
            else
            {
                var defaultLayer = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => l.DefaultLayer).FirstOrDefault();
                if (defaultLayer != null)
                    stack.AddLayer(defaultLayer, Palette);
            }
        }
    }
}
