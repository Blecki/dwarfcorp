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
    public class Crate : GameComponent
    {
        [EntityFactory("Crate")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Crate(Manager, Position);
        }

        private static GeometricPrimitive SharedPrimitive = null;

        public Crate()
        {
        }

        public Crate(ComponentManager manager, Vector3 position) :
            base(manager, "Crate", Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Tags.Add("Crate");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            if (SharedPrimitive == null)
            {
                var spriteSheet = new NamedImageFrame("Terrain\\terrain_tiles");
                SharedPrimitive = new OldBoxPrimitive(DwarfGame.GuiSkin.Device, 0.9f, 0.9f, 0.9f,
                        new OldBoxPrimitive.BoxTextureCoords(spriteSheet.SafeGetImage().Width, spriteSheet.SafeGetImage().Height,
                            new OldBoxPrimitive.FaceData(new Rectangle(224, 0, 32, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(224, 0, 32, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(256, 0, 32, 32), false),
                            new OldBoxPrimitive.FaceData(new Rectangle(224, 0, 32, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(224, 0, 32, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(224, 0, 32, 32), true)));
            }

            AddChild(new PrimitiveComponent(Manager,
                Matrix.CreateRotationY(MathFunctions.Rand(-0.25f, 0.25f)),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.5f, 0.5f, 0.5f),
                SharedPrimitive,
                "Terrain\\terrain_tiles"))
                .SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
