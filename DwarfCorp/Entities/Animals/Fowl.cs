using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Chicken : Creature
    {
        [EntityFactory("Chicken")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Chicken", 32, 32);
        }

        [EntityFactory("Turkey")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Turkey", 48, 48);
        }

        [EntityFactory("Penguin")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Chicken(Position, Manager, "Penguin", 32, 32);
        }

        [JsonProperty] private String Asset = "";
        [JsonProperty] private int SpriteWidth = 32;
        [JsonProperty] private int SpriteHeight = 32;

        public Chicken()
        {

        }

        public Chicken(Vector3 position, ComponentManager manager, string Asset, int SpriteWidth, int SpriteHeight) :
            // Creature base constructor
            base
            (
                manager,
                new CreatureStats(Asset, "Fowl", null),
                manager.World.Factions.Factions["Herbivore"],
                Asset
            )
        {
            Physics = new Physics
                (
                manager,
                    Asset,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.25f, 0.25f, 0.25f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);
            this.Asset = Asset;
            this.SpriteWidth = SpriteWidth;
            this.SpriteHeight = SpriteHeight;
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));
            Physics.AddChild(new PacingCreatureAI(Manager, "AI", Sensor));
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add(Asset);
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the " + Asset;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet("Entities\\Animals\\" + Asset, SpriteWidth, SpriteHeight);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.5f, 0));
            sprite.SpriteSheet = spriteSheet;

            var anims = Library.LoadNewLayeredAnimationFormat("Entities\\Animals\\fowl-animations.json");
            sprite.SetAnimations(anims);

            Physics.AddChild(sprite);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(Shadow.Create(0.5f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_2 };
            NoiseMaker.Noises["Chirp"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_chicken_neutral_2 };
            NoiseMaker.Noises["Lay Egg"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_chicken_lay_egg };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_chicken_hurt_1
            }).SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(new ParticleTrigger("feather", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 10
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

    }
}