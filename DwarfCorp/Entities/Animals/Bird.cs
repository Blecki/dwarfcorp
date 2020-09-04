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
    public class Bird : Creature
    {
        private static string GetRandomBird()
        {
            return ContentPaths.Entities.Animals.Birds.bird_prefix + MathFunctions.Random.Next(8);
        }

        [EntityFactory("Bird")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bird(GetRandomBird(), Position, Manager, "Bird");
        }

        public string SpriteAsset { get; set; }

        public Bird()
        {
            
        }

        public Bird(string sprites, Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats("Bird", "Bird", null),
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            Physics = new Physics
                (
                manager,
                    "A Bird",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.Orientation = Physics.OrientMode.RotateY;

            Physics.AddChild(this);


            SpriteAsset = sprites;

            CreateCosmeticChildren(Manager);

            // Used to sense hostile creatures
             Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));

            // Controls the behavior of the creature
            Physics.AddChild(new BirdAI(Manager, "Bird AI", Sensor));
            
            // The bird can hold one item at a time in its inventory
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add("Bird");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the bird";

            AI.Movement.CanFly = true;
            AI.Movement.CanWalk = false;
            AI.Movement.CanClimb = false;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet(SpriteAsset, 24, 16);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.35f, 0));
            sprite.SpriteSheet = spriteSheet;

            var anims = Library.LoadNewLayeredAnimationFormat("Entities\\Animals\\Birds\\bird-animations.json");
            sprite.SetAnimations(anims);

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
    }
}
