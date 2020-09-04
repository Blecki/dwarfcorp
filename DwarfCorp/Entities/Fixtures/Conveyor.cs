using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DwarfCorp
{
    public class Conveyor : CraftedBody
    {
        [EntityFactory("Conveyor")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Conveyor(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Conveyor()
        {
        }

        public Conveyor(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Conveyor", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(manager, Resource))
        {
            Tags.Add("Conveyor");
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.conveyor, 32);

            var frames = new List<Point>
            {
                new Point(0, 0),
                new Point(1, 0),
                new Point(2, 0),
                new Point(3, 0)
            };

            var forgeAnimation = Library.CreateAnimation(frames, "ConveyorAnimation");
            forgeAnimation.Loops = true;

            var sprite = AddChild(new AnimatedSprite(Manager, "sprite", Matrix.CreateRotationX((float)Math.PI * 0.5f) * Matrix.CreateTranslation(0.0f, -0.4f, 0.0f))
            {
                OrientationType = AnimatedSprite.OrientMode.Fixed
            }) as AnimatedSprite;

            sprite.AddAnimation(forgeAnimation);
            sprite.SpriteSheet = spriteSheet;
            sprite.AnimPlayer.Play(forgeAnimation);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }

        override public void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            base.Update(Time, Chunks, Camera);

            var pushVector = Vector3.UnitZ;
            Quaternion rot;
            Vector3 scale;
            Vector3 trans;
            GlobalTransform.Decompose(out scale, out rot, out trans);

            pushVector = Vector3.Transform(pushVector, rot * Quaternion.CreateFromAxisAngle(Vector3.UnitY, -(float)Math.PI / 2.0f));
            pushVector *= (float)Time.ElapsedGameTime.TotalSeconds * 4.0f;

            foreach (var body in Manager.World.EnumerateIntersectingRootObjects(GetBoundingBox(), CollisionType.Dynamic))
                if (GetBoundingBox().Contains(body.LocalPosition) == ContainmentType.Contains)
                    body.LocalPosition += pushVector;
        }
    }
}
