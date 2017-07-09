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
    public class Bookshelf : Body, IRenderableComponent
    {
        public Bookshelf()
        {
            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Bookshelf(ComponentManager manager, Vector3 position) :
            base(manager, "Bookshelf", Matrix.CreateTranslation(position), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.5f, 0.5f, 0.5f))
        {
            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;

            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bookshelf);
            AddChild(new Box(manager, "model", Matrix.CreateTranslation(new Vector3(-20.0f / 64.0f, -32.0f / 64.0f, -8.0f / 64.0f)), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.0f, 0.0f, 0.0f), "bookshelf", spriteSheet));

            var voxelUnder = new VoxelHandle();

            if (manager.World.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
                AddChild(new VoxelListener(manager.World.ComponentManager, manager.World.ChunkManager, voxelUnder));

            OrientToWalls();
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            effect.SelectionBufferColor = GetGlobalIDColor().ToVector4();
            GetComponent<Box>().Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            // Only renders to selection buffer
        }
    }
}
