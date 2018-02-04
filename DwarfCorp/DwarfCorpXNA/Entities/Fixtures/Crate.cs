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
    public class Crate : Body
    {
        [EntityFactory("Crate")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Crate(Manager, Position);
        }

        public Crate()
        {
            Tags.Add("Crate");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Crate(ComponentManager manager, Vector3 position) :
            base(manager, "Crate", Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Tags.Add("Crate");
            CollisionType = CollisionManager.CollisionType.Static;

            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);

            AddChild(new Box(Manager,
                "Cratebox",
                Matrix.CreateRotationY(MathFunctions.Rand(-0.25f, 0.25f)),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.5f, 0.5f, 0.5f),
                "crate",
                spriteSheet)).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
