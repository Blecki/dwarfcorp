using DwarfCorp.GameStates;

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
            public static string chew = Program.CreatePath("Audio", "chew");
            public static string explode = Program.CreatePath("Audio", "explode");
            public static string fire = Program.CreatePath("Audio", "fire");
            public static string gravel = Program.CreatePath("Audio", "gravel");
            public static string hit = Program.CreatePath("Audio", "hit");
            public static string jump = Program.CreatePath("Audio", "jump");
            public static string ouch = Program.CreatePath("Audio", "ouch");
            public static string pick = Program.CreatePath("Audio", "pick");
            public static string river = Program.CreatePath("Audio", "river");
            public static string sword = Program.CreatePath("Audio", "sword");
            public static string dig = Program.CreatePath("Audio", "dig");
            public static string whoosh = Program.CreatePath("Audio", "whoosh");
            public static string cash = Program.CreatePath("Audio", "cash");
            public static string change = Program.CreatePath("Audio", "change");
            public static string bird = Program.CreatePath("Audio", "bird");
            public static string pluck = Program.CreatePath("Audio", "pluck");
            public static string trap = Program.CreatePath("Audio", "trap");
            public static string vegetation_break = Program.CreatePath("Audio", "vegetation_break");
            public static string hammer = Program.CreatePath("Audio", "hammer");

        }
        public class Particles
        {
            public static string splash = Program.CreatePath("Particles", "splash");
            public static string blood_particle = Program.CreatePath("Particles", "blood_particle");
            public static string dirt_particle = Program.CreatePath("Particles", "dirt_particle");
            public static string flame = Program.CreatePath("Particles", "flame");
            public static string leaf = Program.CreatePath("Particles", "leaf");
            public static string puff = Program.CreatePath("Particles", "puff");
            public static string sand_particle = Program.CreatePath("Particles", "sand_particle");
            public static string splash2 = Program.CreatePath("Particles", "splash2");
            public static string stone_particle = Program.CreatePath("Particles", "stone_particle");
            public static string green_flame = Program.CreatePath("Particles", "green_flame");

        }
        public class Effects
        {
            public static string shadowcircle = Program.CreatePath("Effects", "shadowcircle");
            public static string selection_circle = Program.CreatePath("Effects", "selection_circle");
            public static string slice = Program.CreatePath("Effects", "slice");
            public static string claws = Program.CreatePath("Effects", "claws");
            public static string flash = Program.CreatePath("Effects", "flash");
            public static string rings = Program.CreatePath("Effects", "ring");
        }
        public class Entities
        {
            public class Animals
            {
                public class Birds
                {
                    public static string bird_prefix = Program.CreatePath("Entities", "Animals", "Birds", "bird");
                    public static string bird0 = Program.CreatePath("Entities", "Animals", "Birds", "bird0");
                    public static string bird1 = Program.CreatePath("Entities", "Animals", "Birds", "bird1");
                    public static string bird2 = Program.CreatePath("Entities", "Animals", "Birds", "bird2");
                    public static string bird3 = Program.CreatePath("Entities", "Animals", "Birds", "bird3");
                    public static string bird4 = Program.CreatePath("Entities", "Animals", "Birds", "bird4");
                    public static string bird5 = Program.CreatePath("Entities", "Animals", "Birds", "bird5");
                    public static string bird6 = Program.CreatePath("Entities", "Animals", "Birds", "bird6");
                    public static string bird7 = Program.CreatePath("Entities", "Animals", "Birds", "bird7");

                    // Generates a random bird asset string from bird0 to bird7.
                    public static string GetRandomBird()
                    {
                        return bird_prefix + PlayState.Random.Next(8);
                    }
                }

                public class Deer
                {
                    public static string deer = Program.CreatePath("Entities", "Animals", "Deer", "deer");
                }

                public class Snake
                {
                    public static string snake = Program.CreatePath("Entities", "Animals", "Snake", "snake");
                }
            }

            public class Balloon
            {
                public class Sprites
                {
                    public static string balloon = Program.CreatePath("Entities", "Balloon", "Sprites", "balloon");

                }

            }
            public class Dwarf
            {
                public class Audio
                {
                    public static string dwarfhurt1 = Program.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt1");
                    public static string dwarfhurt2 = Program.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt2");
                    public static string dwarfhurt3 = Program.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt3");
                    public static string dwarfhurt4 = Program.CreatePath("Entities", "Dwarf", "Audio", "dwarfhurt4");

                }
                public class Sprites
                {
                    public static string dwarf_animations = Program.CreatePath("Entities", "Dwarf", "Sprites", "dwarf_animations");
                    public static string soldier_axe_shield= Program.CreatePath("Entities", "Dwarf", "Sprites", "soldier-axe-shield");
                    public static string dwarf_craft = Program.CreatePath("Entities", "Dwarf", "Sprites", "dwarf-craft");
                    public static string dwarf_wizard = Program.CreatePath("Entities", "Dwarf", "Sprites", "dwarf-wizard");
                    public static string dwarf_musket = Program.CreatePath("Entities", "Dwarf", "Sprites", "dwarf-musket");
                }

            }

            public class DwarfObjects
            {
                public static string coinpiles = Program.CreatePath("Entities", "DwarfObjects", "coinpiles");
                public static string beartrap = Program.CreatePath("Entities", "DwarfObjects", "beartrap");
            }

            public class Furniture
            {
                public static string bedtex = Program.CreatePath("Entities", "Furniture", "bedtex");
                public static string interior_furniture = Program.CreatePath("Entities", "Furniture", "interior_furniture");

            }
            public class Goblin
            {

                public class Sprites
                {
                    public static string goblin_withsword = Program.CreatePath("Entities", "Goblin", "Sprites", "gob-withsword");                    
                }

                public class Audio
                {
                    public static string goblinhurt1 = Program.CreatePath("Entities", "Goblin", "Audio", "goblinhurt1");
                    public static string goblinhurt2 = Program.CreatePath("Entities", "Goblin", "Audio", "goblinhurt2");
                    public static string goblinhurt3 = Program.CreatePath("Entities", "Goblin", "Audio", "goblinhurt3");
                    public static string goblinhurt4 = Program.CreatePath("Entities", "Goblin", "Audio", "goblinhurt4");

                }
            }

            public class Skeleton
            {
                public class Sprites
                {
                    public static string skele = Program.CreatePath("Entities", "Skeleton", "skele");
                    public static string necro = Program.CreatePath("Entities", "Skeleton", "necro");
                }
            }
            public class Plants
            {
                public static string berrybush = Program.CreatePath("Entities", "Plants", "berrybush");
                public static string deadbush = Program.CreatePath("Entities", "Plants", "deadbush");
                public static string flower = Program.CreatePath("Entities", "Plants", "flower");
                public static string frostgrass = Program.CreatePath("Entities", "Plants", "frostgrass");
                public static string gnarled = Program.CreatePath("Entities", "Plants", "gnarled");
                public static string grass = Program.CreatePath("Entities", "Plants", "grass");
                public static string mushroom = Program.CreatePath("Entities", "Plants", "mushroom");
                public static string palm = Program.CreatePath("Entities", "Plants", "palm");
                public static string pine = Program.CreatePath("Entities", "Plants", "pine");
                public static string shrub = Program.CreatePath("Entities", "Plants", "shrub");
                public static string snowpine = Program.CreatePath("Entities", "Plants", "snowpine");
                public static string vine = Program.CreatePath("Entities", "Plants", "vine");
                public static string wheat = Program.CreatePath("Entities", "Plants", "wheat");

            }
            public class Resources
            {
                public static string resources = Program.CreatePath("Entities", "Resources", "resources");

            }

        }
        public class Fonts
        {
            public static string Default = Program.CreatePath("Fonts", "Default");
            public static string Small = Program.CreatePath("Fonts", "Small");
            public static string Title = Program.CreatePath("Fonts", "Title");

        }
        public class Gradients
        {
            public static string ambientgradient = Program.CreatePath("Gradients", "ambientgradient");
            public static string skygradient = Program.CreatePath("Gradients", "skygradient");
            public static string sungradient = Program.CreatePath("Gradients", "sungradient");
            public static string torchgradient = Program.CreatePath("Gradients", "torchgradient");

        }
        public class GUI
        {
            public static string gui_widgets = Program.CreatePath("GUI", "gui_widgets");
            public static string icons = Program.CreatePath("GUI", "icons");
            public static string indicators = Program.CreatePath("GUI", "indicators");
            public static string map_icons = Program.CreatePath("GUI", "map_icons");
            public static string pointers = Program.CreatePath("GUI", "pointers");
            public static string room_icons = Program.CreatePath("GUI", "room_icons");
            public static string gui_minimap = Program.CreatePath("GUI", "gui_minimap");
        }
        public class Logos
        {
            public static string companylogo = Program.CreatePath("Logos", "companylogo");
            public static string gamelogo = Program.CreatePath("Logos", "gamelogo");
            public static string grebeardlogo = Program.CreatePath("Logos", "grebeardlogo");
            public static string logos = Program.CreatePath("Logos", "logos");

        }
        public class Models
        {
            public static string sphereLowPoly = Program.CreatePath("Models", "sphereLowPoly");

        }
        public class Music
        {
            public static string dwarfcorp = Program.CreatePath("Music", "dwarfcorp");

        }
        public class Shaders
        {
            public static string BloomCombine = Program.CreatePath("Shaders", "BloomCombine");
            public static string BloomExtract = Program.CreatePath("Shaders", "BloomExtract");
            public static string GaussianBlur = Program.CreatePath("Shaders", "GaussianBlur");
            public static string SkySphere = Program.CreatePath("Shaders", "SkySphere");
            public static string TexturedShaders = Program.CreatePath("Shaders", "TexturedShaders");

        }
        public class Sky
        {
            public static string day_sky = Program.CreatePath("Sky", "day_sky");
            public static string moon = Program.CreatePath("Sky", "moon");
            public static string night_sky = Program.CreatePath("Sky", "night_sky");
            public static string sun = Program.CreatePath("Sky", "sun");

        }
        public class Terrain
        {
            public static string cartoon_water = Program.CreatePath("Terrain", "cartoon_water");
            public static string foam = Program.CreatePath("Terrain", "foam");
            public static string lava = Program.CreatePath("Terrain", "lava");
            public static string lavafoam = Program.CreatePath("Terrain", "lavafoam");
            public static string terrain_illumination = Program.CreatePath("Terrain", "terrain_illumination");
            public static string terrain_tiles = Program.CreatePath("Terrain", "terrain_tiles");
            public static string terrain_colormap = Program.CreatePath("Terrain", "terrain_colormap");
            public static string water_normal = Program.CreatePath("Terrain", "water_normal");
            public static string water_normal2 = Program.CreatePath("Terrain", "water_normal2");

        }

    }

}