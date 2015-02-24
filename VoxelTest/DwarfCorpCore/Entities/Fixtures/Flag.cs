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
    public class Flag : Body
    {
        public Flag()
        {

        }

        public override void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            base.Update(DwarfTime, chunks, camera);
        }

        public Flag(Vector3 position) :
            base("Flag", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            List<Point> frames = new List<Point>
            {
                new Point(0, 2),
                new Point(1, 2),
                new Point(2, 2)
            };
            Animation lampAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "Flag", 32, 32, frames, true, Color.White, 5.0f + MathFunctions.Rand(), 1f, 1.0f, false);

            Sprite sprite = new Sprite(PlayState.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.YAxis
            };
            sprite.AddAnimation(lampAnimation);



            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            lampAnimation.Play();
            Tags.Add("Flag");

            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
