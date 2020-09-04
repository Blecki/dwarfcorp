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
        public MaybeNull<Loadout> Loadout { get; set; }
        public string Name { get; set; }
        public string CoverLetter { get; set; }
        public string FormerProfession { get; set; }
        public string HomeTown { get; set; }
        public string Biography { get; set; }
        public Gender Gender { get; set; }
        public int RandomSeed { get; set; }

        // Todo: Give a bonus to managers.
        public DwarfBux SigningBonus = GameSettings.Current.DwarfBasePay * GameSettings.Current.DwarfSigningBonusFactor;
        public DwarfBux BasePay = GameSettings.Current.DwarfBasePay;

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

        public static Applicant Random(MaybeNull<Loadout> Loadout, CompanyInformation info)
        {
            var r = new Applicant();
            r.GenerateRandom(Loadout, 0, info);
            return r;
        }

        public void GenerateRandom(MaybeNull<Loadout> Loadout, int level, CompanyInformation info)
        {
            this.Loadout = Loadout;
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
                + (Loadout.HasValue(out var loadout) ? loadout.Name : "<??>") + ". " + justifications[MathFunctions.Random.Next(justifications.Count)] +" \n",
                                             "${Thanks,Sincerely,Yours,--,Always}", ",\n    " ,Name);

            FormerProfession = TextGenerator.GenerateRandom("$profession");

            
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.location);
             HomeTown = TextGenerator.GenerateRandom(new List<string>(),
                 templates[MathFunctions.Random.Next(templates.Count)].ToArray());

            Biography = GenerateBiography(Name, Gender);
        }

        public DwarfSprites.LayerStack GetLayers()
        {
            CreatureStats stats = new CreatureStats("Dwarf", "Dwarf", Loadout)
            {
                Gender = this.Gender,
                RandomSeed = RandomSeed
            };

            return DwarfSprites.DwarfBuilder.CreateDwarfLayerStack(stats, Loadout);
        }

        public AnimationPlayer GetAnimationPlayer(DwarfSprites.LayerStack stack, String Anim = "IdleFORWARD")
        {
            foreach (var animation in Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
                if (animation.Key == Anim)
                    return new AnimationPlayer(animation.Value);
            return null;
        }
    }
}
