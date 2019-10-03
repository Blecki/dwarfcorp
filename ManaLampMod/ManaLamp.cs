using Microsoft.Xna.Framework;
using System.Collections.Generic;
using DwarfCorp;

namespace ManaLampMod
{
    public class ManaLamp : CraftedBody
    {
        [EntityFactory("Mana Lamp")]
        private static DwarfCorp.GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new ManaLamp(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public ManaLamp()
        {

        }

        public ManaLamp(ComponentManager Manager, Vector3 position, Resource Resource) :
            base(Manager, "Lamp", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, Resource))
        {
            Tags.Add("Lamp");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet("mana-lamp", 32);

            List<Point> frames = new List<Point>
            {
                new Point(0, 0),
                new Point(2, 0),
                new Point(1, 0),
                new Point(2, 0)
            };

            var lampAnimation = Library.CreateAnimation(spriteSheet, frames, "ManaLampAnimation");
            lampAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.Identity)
            {
                LightsWithVoxels = false,
                OrientationType = AnimatedSprite.OrientMode.YAxis,
            }) as AnimatedSprite;

            sprite.AddAnimation(lampAnimation);
            sprite.AnimPlayer.Play(lampAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            // This is a hack to make the animation update at least once even when the object is created inactive by the craftbuilder.
            sprite.AnimPlayer.Update(new DwarfTime(), false);

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 8)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
