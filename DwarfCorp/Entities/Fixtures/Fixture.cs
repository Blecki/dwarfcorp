using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Fixture : GameComponent
    {
        public SpriteSheet Asset;
        public Point Frame;
        public SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical;

        public Fixture()
        {
            
        }

        public Fixture(
            ComponentManager Manager, 
            Vector3 position, 
            SpriteSheet asset, 
            Point frame,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(
                Manager, 
                "Fixture", 
                Matrix.CreateTranslation(position), 
                new Vector3(asset.FrameWidth / 32.0f, asset.FrameHeight / 32.0f, asset.FrameWidth / 32.0f) * 0.9f, 
                Vector3.Zero)
        {
            DebugColor = Microsoft.Xna.Framework.Color.Salmon;

            Asset = asset;
            Frame = frame;
            CollisionType = CollisionType.Static;
            this.OrientMode = OrientMode;

            AddChild(new Health(Manager, "Hp", 10, 0, 10));

            PropogateTransforms();
            CreateCosmeticChildren(Manager);
        }

        public Fixture(
            String Name,
            IEnumerable<String> Tags,
            ComponentManager Manager,
            Vector3 position,
            SpriteSheet asset,
            Point frame,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            this(Manager, position, asset, frame, OrientMode)
        {
            DebugColor = Microsoft.Xna.Framework.Color.Salmon;

            this.Name = Name;
            this.Tags.AddRange(Tags);
        }
        
        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            AddChild(new SimpleSprite(manager, "Sprite", Matrix.Identity, Asset, Frame)
            {
                OrientationType = OrientMode
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.25f, 0.25f, 0.25f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }            
    }
}
