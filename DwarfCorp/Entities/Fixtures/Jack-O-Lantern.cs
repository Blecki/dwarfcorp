using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class JackOLantern : CraftedBody
    {
        [EntityFactory("Jack-O-Lantern")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new JackOLantern(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public JackOLantern()
        {

        }

        public JackOLantern(ComponentManager Manager, Vector3 position, Resource Resource) :
            base(Manager, "Jack-O-Lantern", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, Resource))
        {
            Tags.Add("Jack-O-Lantern");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            PropogateTransforms();

            var spriteSheet = new SpriteSheet("Entities\\jack-o-lantern", 32);

            List<Point> frames = new List<Point>
            {
                new Point(2, 0),
                new Point(3, 0)
            };

            var jackAnimation = Library.CreateAnimation(frames, "Jack-O-Lantern Animation");
            jackAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.Identity)
            {
                LightsWithVoxels = false,
                OrientationType = AnimatedSprite.OrientMode.YAxis,
            }) as AnimatedSprite;

            sprite.AddAnimation(jackAnimation);
            sprite.SpriteSheet = spriteSheet;
            sprite.AnimPlayer.Play(jackAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            // This is a hack to make the animation update at least once even when the object is created inactive by the craftbuilder.
            sprite.AnimPlayer.Update(new DwarfTime());

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 32)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new RadiusBuffer(Manager, "light buff", Matrix.Identity, new Vector3(8, 8, 8), Vector3.Zero)
            {
                Buff = new StatBuff(1.0f, new StatAdjustment(1) { Name = "JACK-O-LANTERN" })
            }).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
