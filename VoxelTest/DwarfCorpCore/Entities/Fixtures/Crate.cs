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
            
        }

        public Crate(Vector3 position) :
            base("Crate", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles);

            Box crateModel = new Box(PlayState.ComponentManager, "Cratebox", this, Matrix.CreateRotationY(MathFunctions.Rand(-0.25f, 0.5f)), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.5f, 0.5f, 0.5f), PrimitiveLibrary.BoxPrimitives["crate"], spriteSheet);

            Tags.Add("Crate");
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
