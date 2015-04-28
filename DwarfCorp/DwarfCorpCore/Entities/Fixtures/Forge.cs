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
    public class Forge : Body
    {
        public Forge()
        {

        }

        public Forge(Vector3 position) :
            base("Forge", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);

            List<Point> frames = new List<Point>
            {
                new Point(1, 3),
                new Point(3, 3),
                new Point(2, 3),
                new Point(3, 3)
            };
            Animation lampAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "Forge", 32, 32, frames, true, Color.White, 3.0f, 1f, 1.0f, false);

            Sprite sprite = new Sprite(PlayState.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                LightsWithVoxels = false
            };
            sprite.AddAnimation(lampAnimation);


            lampAnimation.Play();
            Tags.Add("Forge");


            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            new LightEmitter("light", this, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 50, 4)
            {
                HasMoved = true
            };
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
