using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Chair : Body
    {
        private void Initialize(ComponentManager manager)
        {
            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            Point topFrame = new Point(2, 6);
            Point sideFrame = new Point(3, 6);

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

            Sprite tabletopSprite = AddChild(new Sprite(manager, "sprite1", Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            tabletopSprite.AddAnimation(tableTop);

            Sprite sprite = AddChild(new Sprite(manager, "sprite", Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = AddChild(new Sprite(manager, "sprite2", Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            sprite2.AddAnimation(tableAnimation);

            

            tableAnimation.Play();
            Tags.Add("Chair");
            CollisionType = CollisionManager.CollisionType.Static;

        }

        public Chair()
        {
        }

        public Chair(ComponentManager manager, Vector3 position) :
            base(manager, "Chair", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            var matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position - new Vector3(0, 0.22f, 0);
            LocalTransform = matrix;

            Initialize(Manager);

            // Todo: Clean up when VoxelListener can take TemporaryVoxelHandles.
            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new TemporaryVoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager,
                    new VoxelHandle(voxelUnder.Coordinate.GetLocalVoxelCoordinate(), voxelUnder.Chunk)));

        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            //Initialize(manager);
            base.CreateCosmeticChildren(manager);
        }
    }
}
