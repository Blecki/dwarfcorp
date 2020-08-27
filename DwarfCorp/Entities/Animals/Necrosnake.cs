using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Necrosnake : Creature
    {
        [EntityFactory("Necrosnake")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Necrosnake(Position, Manager, "Snake");
        }

        public class TailSegment
        {
            public GameComponent Sprite;
            public Vector3 Target;
        }

        [JsonIgnore]
        public List<TailSegment> Tail;

        public Necrosnake()
        {
        }

        public Necrosnake(Vector3 position, ComponentManager manager, string name) :
            base
            (
                manager,
                new CreatureStats("Necrosnake", "Necrosnake", null),
                manager.World.Factions.Factions["Evil"],
                name
            )
        {
            Physics = new Physics
                (
                    manager,
                    name,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                );

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.Fixed;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(5, 5, 5), Vector3.Zero));

            Physics.AddChild(new PacingCreatureAI(Manager, "snake AI", Sensor));

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Snake");
            Physics.Tags.Add("Animal");
            AI.Movement.SetCan(MoveType.ClimbWalls, true);
            AI.Movement.SetCan(MoveType.Dig, true);
            AI.Stats.FullName = "Giant Snake";

            Physics.AddChild(new Flammable(Manager, "Flames"));
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateSprite(ContentPaths.Entities.Animals.Snake.bonesnake_animation, Manager, 0.35f);

            #region Create Tail Pieces

            Tail = new List<TailSegment>();
            var tailAnimations = Library.LoadCompositeAnimationSet(ContentPaths.Entities.Animals.Snake.bonetail_animation, "Necrosnake");

            for (int i = 0; i < 10; ++i)
            {
                var tailPiece = new CharacterSprite(Manager, "Sprite", Matrix.CreateTranslation(0, 0.25f, 0));
                tailPiece.SetAnimations(tailAnimations);

                tailPiece.SetFlag(Flag.ShouldSerialize, false);

                tailPiece.Name = "Snake Tail";
                Tail.Add(
                    new TailSegment()
                    {
                        Sprite = Manager.RootComponent.AddChild(tailPiece) as GameComponent,
                        Target = Physics.LocalTransform.Translation
                    });


                tailPiece.AddChild(new Shadow(Manager));

                var inventory = tailPiece.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset)) as Inventory;
                inventory.SetFlag(Flag.ShouldSerialize, false);
                if (Stats.CurrentClass.HasValue(out var c))
                    inventory.AddResource(new Resource("Bone") { DisplayName = c.Name + " Bone" });
            }

            #endregion

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 4))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>() { ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1 };
            NoiseMaker.Noises["Chirp"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_1,
                ContentPaths.Audio.Oscar.sfx_oc_giant_snake_neutral_2
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_oc_giant_snake_hurt_1,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(Manager);
        }

        public override void Die()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.Die();
            }
            base.Die();
        }

        public override void Delete()
        {
            foreach (var tail in Tail)
            {
                tail.Sprite.GetRoot().Delete();
            }
            base.Delete();

        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if ((Physics.Position - Tail.First().Target).LengthSquared() > 0.5f)
            {
                for (int i = Tail.Count - 1; i > 0; i--)
                {
                    Tail[i].Target = Tail[i - 1].Target;
                }
                Tail[0].Target = Physics.Position;
            }

            int k = 0;
            foreach (var tail in Tail)
            {
                Vector3 diff = Vector3.UnitX;
                if (k == Tail.Count - 1)
                {
                    diff = AI.Position - tail.Sprite.LocalPosition;
                }
                else
                {
                    diff = Tail[k + 1].Sprite.LocalPosition - tail.Sprite.LocalPosition;
                }
                var mat = Matrix.CreateRotationY((float)Math.Atan2(diff.X, -diff.Z));
                mat.Translation = 0.9f * tail.Sprite.LocalPosition + 0.1f * tail.Target;
                tail.Sprite.LocalTransform = mat;
                tail.Sprite.UpdateTransform();
                tail.Sprite.PropogateTransforms();
                k++;
            }
            base.Update(gameTime, chunks, camera);
        }

    }
}
