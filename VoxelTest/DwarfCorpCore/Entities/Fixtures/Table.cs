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
    public class Table : Body
    {
        public Table()
        {
            
        }

        public Table(Vector3 position):
            this(position, null, Point.Zero)
        {
            
        }

        public Table(Vector3 position, string asset) :
            this(position, new SpriteSheet(asset), Point.Zero)
        {

        }

        public Table(Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame) :
            base("Table", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.interior_furniture);
            Point topFrame = new Point(0, 6);
            Point sideFrame = new Point(1, 6);

            List<Point> frames = new List<Point>
            {
                topFrame
            };

            List<Point> sideframes = new List<Point>
            {
                sideFrame
            };

            Animation tableTop = new Animation(GameState.Game.GraphicsDevice, spriteSheet, "tableTop", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);
            Animation tableAnimation = new Animation(GameState.Game.GraphicsDevice, spriteSheet, "tableTop", 32, 32, sideframes, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite tabletopSprite = new Sprite(componentManager, "sprite1", this, Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            tabletopSprite.AddAnimation(tableTop);

            Sprite sprite = new Sprite(componentManager, "sprite", this, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(componentManager, "sprite2", this, Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }


            tableAnimation.Play();
            Tags.Add("Table");
            CollisionType = CollisionManager.CollisionType.Static;

            if (fixtureAsset != null)
            {
                new Fixture(new Vector3(0, 0.3f, 0), fixtureAsset, fixtureFrame, this);
            }
        }
    }
}
