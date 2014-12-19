using System.Collections.Generic;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    /// <summary>
    /// This class is auto-generated. It exists to allow intellisense and compile-time awareness
    /// for content. This is to prevent inlining of file paths and mis-spellings.
    /// </summary>
    public class ContentPaths
    {
        public class Audio
        {
            public static string chew = ProgramData.CreatePath("Audio", "chew");
            public static string explode = ProgramData.CreatePath("Audio", "explode");
            public static string fire = ProgramData.CreatePath("Audio", "fire");
            public static string gravel = ProgramData.CreatePath("Audio", "gravel");
            public static string hit = ProgramData.CreatePath("Audio", "hit");
            public static string jump = ProgramData.CreatePath("Audio", "jump");
            public static string ouch = ProgramData.CreatePath("Audio", "ouch");
            public static string pick = ProgramData.CreatePath("Audio", "pick");
            public static string river = ProgramData.CreatePath("Audio", "river");
            public static string sword = ProgramData.CreatePath("Audio", "sword");
            public static string dig = ProgramData.CreatePath("Audio", "dig");
            public static string whoosh = ProgramData.CreatePath("Audio", "whoosh");
            public static string cash = ProgramData.CreatePath("Audio", "cash");
            public static string change = ProgramData.CreatePath("Audio", "change");
            public static string bird = ProgramData.CreatePath("Audio", "bird");
            public static string pluck = ProgramData.CreatePath("Audio", "pluck");
            public static string trap = ProgramData.CreatePath("Audio", "trap");
            public static string vegetation_break = ProgramData.CreatePath("Audio", "vegetation_break");
            public static string hammer = ProgramData.CreatePath("Audio", "hammer");

        }
        public class Particles
        {
            public static string splash = ProgramData.CreatePath("Particles", "splash");
            public static string blood_particle = ProgramData.CreatePath("Particles", "blood_particle");
            public static string dirt_particle = ProgramData.CreatePath("Particles", "dirt_particle");
            public static string flame = ProgramData.CreatePath("Particles", "flame");
            public static string leaf = ProgramData.CreatePath("Particles", "leaf");
            public static string puff = ProgramData.CreatePath("Particles", "puff");
            public static string sand_particle = ProgramData.CreatePath("Particles", "sand_particle");
            public static string splash2 = ProgramData.CreatePath("Particles", "splash2");
            public static string stone_particle = ProgramData.CreatePath("Particles", "stone_particle");
            public static string green_flame = ProgramData.CreatePath("Particles", "green_flame");
            public static string star_particle = ProgramData.CreatePath("Particles", "bigstar_particle");
        }
        public class Effects
        {
            public static string shadowcircle = ProgramData.CreatePath("Effects", "shadowcircle");
            public static string selection_circle = ProgramData.CreatePath("Effects", "selection_circle");
            public static string slice = ProgramData.CreatePath("Effects", "slice");
            public static string claws = ProgramData.CreatePath("Effects", "claws");
            public static string flash = ProgramData.CreatePath("Effects", "flash");
            public static string rings = ProgramData.CreatePath("Effects", "ring");
        }


        public static string GetFileAsString(string asset)
        {
            string text = "";
            using (var stream = TitleContainer.OpenStream("Content" + ProgramData.DirChar + asset))
            {
                using (var reader = new StreamReader(stream))
                {
                    text = reader.ReadToEnd();
                }
            }
            return text;
        }

        public class Text
        {
            public enum TextType
            {
                Adjectives,
                Animals,
                FirstNames,
                LastNames,
                Nouns,
                Verbs,
                Adverbs
            }

            public static Dictionary<TextType, string> Texts = new Dictionary<TextType, string>()
            {
                {TextType.Adjectives, ProgramData.CreatePath("Text", "adjectives.txt")},
                {TextType.Animals,  ProgramData.CreatePath("Text","animals.txt")},
                {TextType.FirstNames,  ProgramData.CreatePath("Text","firstnames.txt")},
                {TextType.LastNames,  ProgramData.CreatePath("Text","lastnames.txt")},
                {TextType.Nouns,  ProgramData.CreatePath("Text","nouns.txt")},
                {TextType.Verbs,  ProgramData.CreatePath("Text","verbs.txt")},
                {TextType.Adverbs,  ProgramData.CreatePath("Text","adverbs.txt")}
            };
        }

        public class Entities
        {
            public class Animals
            {
                public class Birds
                {
                    public static string bird_prefix = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird");
                    public static string bird0 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird0");
                    public static string bird1 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird1");
                    public static string bird2 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird2");
                    public static string bird3 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird3");
                    public static string bird4 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird4");
                    public static string bird5 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird5");
                    public static string bird6 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird6");
                    public static string bird7 = ProgramData.CreatePath("Entities", "Animals", "Birds", "bird7");

                    // Generates a random bird asset string from bird0 to bird7.
                    public static string GetRandomBird()
                    {
                        return bird_prefix + PlayState.Random.Next(8);
                    }
                }

                public class Deer
                {
                    public static string deer = ProgramData.CreatePath("Entities", "Animals", "Deer", "deer");
                }

                public class Snake
                {
                    public static string snake = ProgramData.CreatePath("Entities", "Animals", "Snake", "snake");
                }
            }

            public class Balloon
            {
                public class Sprites
                {
                    public static string balloon = ProgramData.CreatePath("Entities", "Balloon", "Sprites", "balloon");

                }

            }
            public class Dwarf
            {
                public class Audio
                {
                    public static string dwarfhurt1 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt1");
                    public static string dwarfhurt2 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt2");
                    public static string dwarfhurt3 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt3");
                    public static string dwarfhurt4 = ProgramData.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt4");

                }
                public class Sprites
                {
                    public static string crafter_hammer = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter-hammer");
                    public static string crafter = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter");
                    public static string soldier_axe = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier-axe");
                    public static string soldier_shield = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier-shield");
                    public static string soldier = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier");
                    public static string wizard_staff = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard-staff");
                    public static string wizard = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard");
                    public static string worker_pick = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker-pick");
                    public static string worker = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker");

                    public static string worker_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "worker_animation.json");
                    public static string crafter_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "crafter_animation.json");
                    public static string wizard_animation = ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "wizard_animation.json");
                    public static string soldier_animation =  ProgramData.CreatePath("Entities", "Dwarf", "Sprites", "soldier_animation.json");
                }

            }

            public class DwarfObjects
            {
                public static string coinpiles = ProgramData.CreatePath("Entities", "DwarfObjects", "coinpiles");
                public static string beartrap = ProgramData.CreatePath("Entities", "DwarfObjects", "beartrap");
                public static string underconstruction = ProgramData.CreatePath("Entities", "DwarfObjects", "underconstruction");
                public static string constructiontape = ProgramData.CreatePath("Entities", "DwarfObjects", "constructiontape");
            }

            public class Furniture
            {
                public static string bedtex = ProgramData.CreatePath("Entities", "Furniture", "bedtex");
                public static string interior_furniture = ProgramData.CreatePath("Entities", "Furniture", "interior_furniture");

            }
            public class Goblin
            {

                public class Sprites
                {
                    public static string goblin_withsword = ProgramData.CreatePath("Entities", "Goblin", "Sprites", "gob-withsword");                    
                }

                public class Audio
                {
                    public static string goblinhurt1 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt1");
                    public static string goblinhurt2 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt2");
                    public static string goblinhurt3 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt3");
                    public static string goblinhurt4 = ProgramData.CreatePath("Entities", "Goblin", "Audio", "goblinhurt4");

                }
            }

            public class Skeleton
            {
                public class Sprites
                {
                    public static string skele = ProgramData.CreatePath("Entities", "Skeleton", "skele");
                    public static string necro = ProgramData.CreatePath("Entities", "Skeleton", "necro");
                }
            }
            public class Plants
            {
                public static string berrybush = ProgramData.CreatePath("Entities", "Plants", "berrybush");
                public static string deadbush = ProgramData.CreatePath("Entities", "Plants", "deadbush");
                public static string flower = ProgramData.CreatePath("Entities", "Plants", "flower");
                public static string frostgrass = ProgramData.CreatePath("Entities", "Plants", "frostgrass");
                public static string gnarled = ProgramData.CreatePath("Entities", "Plants", "gnarled");
                public static string grass = ProgramData.CreatePath("Entities", "Plants", "grass");
                public static string mushroom = ProgramData.CreatePath("Entities", "Plants", "mushroom");
                public static string palm = ProgramData.CreatePath("Entities", "Plants", "palm");
                public static string pine = ProgramData.CreatePath("Entities", "Plants", "pine");
                public static string shrub = ProgramData.CreatePath("Entities", "Plants", "shrub");
                public static string snowpine = ProgramData.CreatePath("Entities", "Plants", "snowpine");
                public static string vine = ProgramData.CreatePath("Entities", "Plants", "vine");
                public static string wheat = ProgramData.CreatePath("Entities", "Plants", "wheat");

            }
            public class Resources
            {
                public static string resources = ProgramData.CreatePath("Entities", "Resources", "resources");

            }

        }
        public class Fonts
        {
            public static string Default = ProgramData.CreatePath("Fonts", "font1-w-2x");
            public static string Small = ProgramData.CreatePath("Fonts", "font1-w");
            public static string Title = ProgramData.CreatePath("Fonts", "font1-w-4x");

        }
        public class Gradients
        {
            public static string ambientgradient = ProgramData.CreatePath("Gradients", "ambientgradient");
            public static string skygradient = ProgramData.CreatePath("Gradients", "skygradient");
            public static string sungradient = ProgramData.CreatePath("Gradients", "sungradient");
            public static string torchgradient = ProgramData.CreatePath("Gradients", "torchgradient");

        }
        public class GUI
        {
            public static string gui_widgets = ProgramData.CreatePath("GUI", "gui_widgets");
            public static string icons = ProgramData.CreatePath("GUI", "icons");
            public static string indicators = ProgramData.CreatePath("GUI", "indicators");
            public static string map_icons = ProgramData.CreatePath("GUI", "map_icons");
            public static string pointers = ProgramData.CreatePath("GUI", "pointers");
            public static string room_icons = ProgramData.CreatePath("GUI", "room_icons");
            public static string gui_minimap = ProgramData.CreatePath("GUI", "gui_minimap");
        }
        public class Logos
        {
            public static string companylogo = ProgramData.CreatePath("Logos", "companylogo");
            public static string gamelogo = ProgramData.CreatePath("Logos", "gamelogo");
            public static string grebeardlogo = ProgramData.CreatePath("Logos", "grebeardlogo");
            public static string logos = ProgramData.CreatePath("Logos", "logos");

        }
        public class Models
        {
            public static string sphereLowPoly = ProgramData.CreatePath("Models", "sphereLowPoly");

        }
        public class Music
        {
#if XNA_BUILD
            public static string dwarfcorp = ProgramData.CreatePath("Music", "dwarfcorp");
#else
            public static string dwarfcorp = ProgramData.CreatePath("Music", "dwarfcorp_ogg");
#endif

        }
        public class Shaders
        {
#if XNA_BUILD
            public static string BloomCombine = ProgramData.CreatePath("Shaders", "BloomCombine");
            public static string BloomExtract = ProgramData.CreatePath("Shaders", "BloomExtract");
            public static string GaussianBlur = ProgramData.CreatePath("Shaders", "GaussianBlur");
            public static string SkySphere = ProgramData.CreatePath("Shaders", "SkySphere");
            public static string TexturedShaders = ProgramData.CreatePath("Shaders", "TexturedShaders");
#else
            public static string BloomCombine = ProgramData.CreatePath("Shaders", "BloomCombine_Opengl");
            public static string BloomExtract = ProgramData.CreatePath("Shaders", "BloomExtract_Opengl");
            public static string GaussianBlur = ProgramData.CreatePath("Shaders", "GaussianBlur_Opengl");
            public static string SkySphere = ProgramData.CreatePath("Shaders", "SkySphere_Opengl");
            public static string TexturedShaders = ProgramData.CreatePath("Shaders", "TexturedShaders_Opengl");
#endif

        }
        public class Sky
        {
            public static string day_sky = ProgramData.CreatePath("Sky", "day_sky");
            public static string moon = ProgramData.CreatePath("Sky", "moon");
            public static string night_sky = ProgramData.CreatePath("Sky", "night_sky");
            public static string sun = ProgramData.CreatePath("Sky", "sun");

        }
        public class Terrain
        {
            public static string cartoon_water = ProgramData.CreatePath("Terrain", "cartoon_water");
            public static string foam = ProgramData.CreatePath("Terrain", "foam");
            public static string lava = ProgramData.CreatePath("Terrain", "lava");
            public static string lavafoam = ProgramData.CreatePath("Terrain", "lavafoam");
            public static string terrain_illumination = ProgramData.CreatePath("Terrain", "terrain_illumination");
            public static string terrain_tiles = ProgramData.CreatePath("Terrain", "terrain_tiles");
            public static string terrain_colormap = ProgramData.CreatePath("Terrain", "terrain_colormap");
            public static string water_normal = ProgramData.CreatePath("Terrain", "water_normal");
            public static string water_normal2 = ProgramData.CreatePath("Terrain", "water_normal2");

        }

    }

}