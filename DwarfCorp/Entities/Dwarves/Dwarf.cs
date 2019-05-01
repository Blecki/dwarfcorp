using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class Dwarf : Creature
    {
        [JsonProperty] // Dwarves need to save the class because, unlike every single other creature, the class isn't implied by their type.
        private CreatureClass SavedDwarfEmployeeClass = null;

        public Dwarf()
        {
            
        }

        public Dwarf(ComponentManager manager, CreatureStats stats, string allies, PlanService planService, Faction faction,  string name, CreatureClass workerClass, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Dwarf", Matrix.CreateTranslation(position),
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            HasMeat = false;
            HasBones = false;
            HasCorpse = true;

            SavedDwarfEmployeeClass = workerClass;

            Stats.Gender = Mating.RandomGender();
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Dwarf AI", Sensors));
         
            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            Stats.FindAdjustment("base stats").Size = 5;
            Stats.CanSleep = true;
            Stats.CanEat = true;
            Stats.CanGetBored = true;
            AI.Movement.CanClimbWalls = true; // Why isn't this a flag like the below?
            AI.Movement.SetCan(MoveType.Teleport, true);
            AI.Movement.SetCost(MoveType.Teleport, 1.0f);
            AI.Movement.SetSpeed(MoveType.Teleport, 10.0f);
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.EnterVehicle, true);
            AI.Movement.SetCan(MoveType.ExitVehicle, true);
            AI.Movement.SetCan(MoveType.RideVehicle, true);
            AI.Movement.SetCost(MoveType.EnterVehicle, 0.01f);
            AI.Movement.SetCost(MoveType.ExitVehicle, 0.01f);
            AI.Movement.SetCost(MoveType.RideVehicle, 0.01f);
            AI.Movement.SetSpeed(MoveType.RideVehicle, 3.0f);
            AI.Movement.SetSpeed(MoveType.EnterVehicle, 1.0f);
            AI.Movement.SetSpeed(MoveType.ExitVehicle, 1.0f);
            if (AI.Stats.IsTaskAllowed(Task.TaskCategory.Dig))
                AI.Movement.SetCan(MoveType.Dig, true);
            AI.TriggersMourning = true;
            AI.Biography = Applicant.GenerateBiography(AI.Stats.FullName, Stats.Gender);
            Species = "Dwarf";
            Status.Money = (decimal)MathFunctions.Rand(0, 150);

            Physics.AddChild(new DwarfThoughts(Manager, "Thoughts"));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SavedDwarfEmployeeClass;
            CreateDwarfSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new VoxelRevealer(manager, Physics, 5)).SetFlag(Flag.ShouldSerialize, false);
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_2,
            };

            NoiseMaker.Noises["Ok"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_3
            };

            NoiseMaker.Noises["Die"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_death
            };

            NoiseMaker.Noises["Pleased"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pleased
            };

            NoiseMaker.Noises["Tantrum"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_3,
            };
            NoiseMaker.Noises["Jump"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_jump
            };

            NoiseMaker.Noises["Climb"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_3
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        protected void CreateDwarfSprite(CreatureClass employeeClass, ComponentManager manager)
        {
            if (Physics == null)
            {
                Physics = GetRoot().GetComponent<Physics>();
                if (Physics == null) return;
            }

            var sprite = Physics.AddChild(new LayeredSprites.LayeredCharacterSprite(manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)))) as LayeredSprites.LayeredCharacterSprite;

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("hair")).SelectRandom(random);
            var skinPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("face")).SelectRandom(random);
            AddLayerOrDefault(sprite, random, "body", skinPalette);
            AddLayerOrDefault(sprite, random, "face", skinPalette);
            AddLayerOrDefault(sprite, random, "nose", skinPalette);
            AddLayerOrDefault(sprite, random, "beard", hairPalette);
            AddLayerOrDefault(sprite, random, "hair", hairPalette);
            AddLayerOrDefault(sprite, random, "tool");
            AddLayerOrDefault(sprite, random, "hat", hairPalette);

            AttackMode = Stats.CurrentClass.AttackMode;

            foreach (Animation animation in AnimationLibrary.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
                sprite.AddAnimation(animation);

            sprite.SetCurrentAnimation(Sprite.Animations.First().Value);
            sprite.SetFlag(Flag.ShouldSerialize, false);
        }

        private void AddLayerOrDefault(LayeredSprites.LayeredCharacterSprite Sprite, Random Random, String Layer, LayeredSprites.Palette Palette = null)
        {
            var layers = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => !l.DefaultLayer && l.PassesFilter(this.Stats));
            if (layers.Count() > 0)
            {
                var newLayer = layers.SelectRandom(Random);
                Sprite.AddLayer(newLayer, Palette);
                // Do not allow hats and hair on the same head.
                if (newLayer.Asset != "Entities/Dwarf/Layers/blank" && Layer == "hat")
                {
                    Sprite.RemoveLayer("hair");
                }
            }
            else
            {
                var defaultLayer = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => l.DefaultLayer).FirstOrDefault();
                if (defaultLayer != null)
                    Sprite.AddLayer(defaultLayer, Palette);
            }
        }
    }
}
