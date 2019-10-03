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
    public class Forge : CraftedBody
    {
        [EntityFactory("Forge")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Forge(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Forge()
        {

        }

        public Forge(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Forge", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
        {
            Tags.Add("Forge");
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            var frames = new List<Point>
            {
                new Point(1, 3),
                new Point(3, 3),
                new Point(2, 3),
                new Point(3, 3)
            };

            var forgeAnimation = Library.CreateAnimation(spriteSheet, frames, "ForgeLightAnimation");
            forgeAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.Identity)
            {
                LightsWithVoxels = false
            }) as AnimatedSprite;

            sprite.AddAnimation(forgeAnimation);
            sprite.AnimPlayer.Play(forgeAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            // This is a hack to make the animation update at least once even when the object is created inactive by the craftbuilder.
            sprite.AnimPlayer.Update(new DwarfTime(), false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
