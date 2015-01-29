using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class ResourceEntity : Physics
    {
        public ResourceEntity()
        {
            
        }

        public ResourceEntity(ResourceLibrary.ResourceType resourceType, Vector3 position) :
            base(ResourceLibrary.ResourceNames[resourceType], PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            Resource type = ResourceLibrary.Resources[resourceType];
            SpriteSheet spriteSheet = new SpriteSheet(type.Image.AssetName);

            int frameX = type.Image.SourceRect.X / 32;
            int frameY = type.Image.SourceRect.Y / 32;

            List<Point> frames = new List<Point>
            {
                new Point(frameX, frameY)
            };
            Animation animation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(type.Image.AssetName), "Animation", 32, 32, frames, false, Color.White, 0.01f, 0.5f, 0.5f, false);

            Sprite sprite = new Sprite(PlayState.ComponentManager, "Sprite", this, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Spherical,
                LightsWithVoxels = !type.SelfIlluminating
            };
            sprite.AddAnimation(animation);


            animation.Play();

            Tags.Add(type.ResourceName);
            Tags.Add("Resource");
            Bobber bobber = new Bobber(0.05f, 2.0f, MathFunctions.Rand() * 3.0f, sprite);
        }
    }
}
