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
    public class Bookshelf : CraftedBody, IRenderableComponent
    {
        public bool FrustumCull { get { return true; } }

        public Bookshelf()
        {
            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Bookshelf(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, "Bookshelf", Matrix.CreateTranslation(position), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.5f, 0.5f, 0.5f), new CraftDetails(manager, "Bookshelf", resources))
        {
            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager, voxelUnder));

            CreateCosmeticChildren(manager);
            OrientToWalls();
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            GetComponent<Box>().Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            // Only renders to selection buffer
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bookshelf);

            AddChild(new Box(Manager,
                "model",
                Matrix.CreateTranslation(new Vector3(-20.0f / 64.0f, -32.0f / 64.0f, -8.0f / 64.0f)),
                new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                "bookshelf",
                spriteSheet)).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
