using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Frog : Creature
    {
        [EntityFactory("Frog")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Frog("Entities\\Animals\\Frog\\frog0", Position, Manager, "Frog");
        }

        [EntityFactory("Tree Frog")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Frog("Entities\\Animals\\Frog\\frog1", Position, Manager, "Frog");
        }

        public string SpriteAsset;

        public Frog()
        {

        }

        public Frog(string sprites, Vector3 position, ComponentManager manager, string name) :
            // Creature base constructor
            base
            (
                manager,
                new CreatureStats("Frog", "Frog", null),
                manager.World.Factions.Factions["Herbivore"],
                name
            )
        {
            SpriteAsset = sprites;
            Physics = new Physics
                (
                    manager,
                    "A Frog",
                    Matrix.CreateTranslation(position),
                    new Vector3(0.375f, 0.375f, 0.375f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;

            SpriteAsset = sprites;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));
            Physics.AddChild(new PacingCreatureAI(Manager, "Rabbit AI", Sensor));
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add("Frog");
            Physics.Tags.Add("Animal");
            Physics.Tags.Add("DomesticAnimal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the frog";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet(SpriteAsset, 32, 24);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.35f, 0));

            var anims = Library.LoadNewLayeredAnimationFormat("Entities\\Animals\\Frog\\frog-animations.json");
            foreach (var anim in anims)
                anim.Value.SpriteSheet = spriteSheet;
            sprite.SetAnimations(anims);

            Physics.AddChild(sprite);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(Shadow.Create(0.3f, manager));

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Idle"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_2 };
            NoiseMaker.Noises["Chrip"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_1, ContentPaths.Audio.Oscar.sfx_oc_frog_neutral_2 };
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_1, ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_2 };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_frog_hurt_1
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
