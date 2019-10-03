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
    public class Lamp : CraftedBody
    {
        [EntityFactory("Lamp")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Lamp(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        private enum OrientationType
        {
            Standing,
            Wall,
            None
        }

        private OrientationType _orientationType = OrientationType.None;
        private int _prevdx = -2;
        private int _prevdz = -2;
        public Lamp()
        {

        }

        private void CreateSpriteStanding()
        {
            _orientationType = OrientationType.Standing;
            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);

            List<Point> frames = new List<Point>
            {
                new Point(0, 1),
                new Point(2, 1),
                new Point(1, 1),
                new Point(2, 1)
            };

            var lampAnimation = Library.CreateAnimation(spriteSheet, frames, "LampAnimation");
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
        }

        private void CreateSpriteWall(Vector3 diff)
        {
            AddChild(new SimpleSprite(Manager, "sprite",
                Matrix.CreateTranslation(diff * 0.2f + Vector3.Up * 0.2f),
                new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32),
                new Point(5, 0))
            {
                OrientationType = SimpleSprite.OrientMode.YAxis,
                LightsWithVoxels = false
            }).SetFlag(Flag.ShouldSerialize, false);
            _orientationType = OrientationType.Wall;
        }

        private bool CreateSprite()
        {
            PropogateTransforms();
            var voxel = new VoxelHandle(Manager.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(LocalPosition));
            if (!voxel.IsValid && _orientationType != OrientationType.Standing)
            {
                CreateSpriteStanding();
                return true;
            }

            for (var dx = -1; dx < 2; dx++)
            {
                for (var dz = -1; dz < 2; dz++)
                {
                    if (Math.Abs(dx) + Math.Abs(dz) != 1)
                        continue;

                    var vox = new VoxelHandle(Manager.World.ChunkManager, voxel.Coordinate + new GlobalVoxelOffset(dx, 0, dz));

                    if (vox.IsValid && !vox.IsEmpty)
                    {
                        if (_prevdx == dx && _prevdz == dz && _orientationType == OrientationType.Wall)
                        {
                            return false;
                        }
                        CreateSpriteWall(new Vector3(dx, 0, dz));
                        _prevdx = dx;
                        _prevdz = dz;
                        return true;
                    }
                }
            }

            if (_orientationType != OrientationType.Standing)
            {
                CreateSpriteStanding();
                return true;
            }
            return false;
        }

        public Lamp(ComponentManager Manager, Vector3 position, Resource Resource) :
            base(Manager, "Lamp", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, Resource))
        {
            Tags.Add("Lamp");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            CreateSprite();

            AddChild(new LightEmitter(Manager, "light", Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 255, 16)
            {
                HasMoved = true
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new RadiusBuffer(Manager, "light buff", Matrix.Identity, new Vector3(8, 8, 8), Vector3.Zero)
            {
                Buff = new StatBuff(1.0f, new StatAdjustment(1) { Name = "LAMP" })
            }).SetFlag(Flag.ShouldSerialize, false);
        }

        public override void Orient(float angle)
        {
            base.Orient(angle);
            if (CreateSprite())
            {
                var sprite = EnumerateChildren().First(c => c.Name == "sprite");
                sprite.Delete();
            }
        }
    }
}
