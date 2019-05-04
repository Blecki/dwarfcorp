using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Slime : Creature
    {
        
        private static string GetRandomBird()
        {
            return ContentPaths.Entities.Animals.Slimes[MathFunctions.RandInt(0, ContentPaths.Entities.Animals.Slimes.Count)];
        }

        [EntityFactory("Slime")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Slime(GetRandomBird(), Position, Manager, "Slime");
        }

        public string SpriteAsset { get; set; }

        public Slime()
        {
            
        }

        public Slime(string sprites, Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats
                {
                    BaseDexterity = 6,
                    BaseConstitution = 1,
                    BaseStrength = 1,
                    BaseWisdom = 1,
                    BaseCharisma = 1,
                    BaseIntelligence = 1,
                    BaseSize = 0.25f,
                    CanSleep = false,
                    LaysEggs = true,
                    IsMigratory = true
                },
                "Herbivore",
                manager.World.PlanService,
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            Physics = new Physics
                (
                manager,
                    "Slime",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.Orientation = Physics.OrientMode.RotateY;

            Physics.AddChild(this);


            SpriteAsset = sprites;
            BaseMeatResource = "Slime";

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
             Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new PacingCreatureAI(Manager, "Bird AI", Sensors));
            
            // The bird can peck at its enemies (0.1 damage)
            Attacks = new List<Attack> { new Attack("Peck", 0.1f, 2.0f, 1.0f, SoundSource.Create(ContentPaths.Audio.Oscar.sfx_oc_bird_attack), ContentPaths.Effects.pierce) { Mode = Attack.AttackMode.Dogfight } };

            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            // The bird is flammable, and can die when exposed to fire.
            Physics.AddChild(new Flammable(Manager, "Flames"));

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Animal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bird";

            AI.Movement.CanWalk = true;
            AI.Movement.CanClimbWalls = false;
            AI.Movement.CanSwim = false;
            AI.Movement.SetSpeed(MoveType.Jump, 1.5f);
            AI.Movement.SetSpeed(MoveType.Climb, 1.5f);
            AI.Movement.SetCost(MoveType.Climb, 0.1f);
            Species = "Slime";
            CanReproduce = true;
            BabyType = "Slime";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;

            var spriteSheet = new SpriteSheet(SpriteAsset, 48, 48);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.35f, 0));
            foreach (var animation in AnimationLibrary.LoadNewLayeredAnimationFormat(ContentPaths.Entities.Animals.slime_animations))
            {
                animation.SpriteSheet = spriteSheet;
                sprite.AddAnimation(animation);
            }

            Physics.AddChild(sprite);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(Shadow.Create(0.3f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises.Add("chirp", new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bird_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_bird_neutral_2 });
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bird_hurt };
            NoiseMaker.Noises["Lay Egg"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_bird_lay_egg };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_bird_hurt
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        private static CreatureClass SharedClass = new CreatureClass()
        {
            Name = "Bird",
            Levels = new List<CreatureClass.Level>() { new CreatureClass.Level() { Index = 0, Name = "Bird" } }
        };
    }
}
