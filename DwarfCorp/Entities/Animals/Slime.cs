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
        // Todo: These need split up if I want to allow them to reproduce.
        [EntityFactory("Slime - Blue")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Slime("Entities\\Animals\\Slimes\\slime_blue", "Blue Slime", Position, Manager, "Slime");
        }

        [EntityFactory("Slime - Green")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Slime("Entities\\Animals\\Slimes\\slime_green", "Green Slime", Position, Manager, "Slime");
        }


        [EntityFactory("Slime - Red")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Slime("Entities\\Animals\\Slimes\\slime_red", "Red Slime", Position, Manager, "Slime");
        }

        [EntityFactory("Slime - Yellow")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Slime("Entities\\Animals\\Slimes\\slime_yellow", "Yellow Slime", Position, Manager, "Slime");
        }

        public string SpriteAsset { get; set; }

        public Slime()
        {
            
        }

        public Slime(string sprites, String SlimeType, Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats("Slime", "Slime", 0),
                manager.World.Factions.Factions["Demon"],
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

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));
            Physics.AddChild(new PacingCreatureAI(Manager, "Slime AI", Sensor));
            
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Inventory.AddResource(new ResourceAmount(ResourceLibrary.GetResourceByName(SlimeType), MathFunctions.RandInt(1, 3)));

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.Tags.Add("Animal");
            Stats.FullName = TextGenerator.GenerateRandom("$firstname") + " the slime";

            AI.Movement.CanWalk = true;
            AI.Movement.CanClimbWalls = false;
            AI.Movement.CanSwim = false;
            AI.Movement.SetSpeed(MoveType.Jump, 1.5f);
            AI.Movement.SetSpeed(MoveType.Climb, 1.5f);
            AI.Movement.SetCost(MoveType.Climb, 0.1f);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            var spriteSheet = new SpriteSheet(SpriteAsset, 48, 48);
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, 0.35f, 0));

            var anims = AnimationLibrary.LoadNewLayeredAnimationFormat("Entities\\Animals\\Slimes\\slime-animations.json");
            foreach (var anim in anims)
                anim.Value.SpriteSheet = spriteSheet;
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
