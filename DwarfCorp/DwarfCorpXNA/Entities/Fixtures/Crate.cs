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
    [JsonObject(IsReference = true)]
    public class Crate : Body
    {
        public Crate()
        {
            Tags.Add("Crate");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Crate(ComponentManager manager, Vector3 position) :
            base(manager, "Crate", Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);

            var box = AddChild(new Box(manager, "Cratebox", Matrix.CreateRotationY(MathFunctions.Rand(-0.25f, 0.25f)), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.5f, 0.5f, 0.5f), "crate", spriteSheet));

            box.SetFlag(Flag.ShouldSerialize, false);

            Tags.Add("Crate");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);
            var box = AddChild(new Box(manager, "Cratebox", Matrix.CreateRotationY(MathFunctions.Rand(-0.25f, 0.25f)), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.5f, 0.5f, 0.5f), "crate", spriteSheet));

            box.SetFlag(Flag.ShouldSerialize, false);
            base.CreateCosmeticChildren(manager);
        }
    }
}
