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
    public class Bed : Body
    {
        private Box bedModel;
        public Bed()
        {
            
        }

        public Bed(Vector3 position) :
            base("Bed", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);
            bedModel = new Box(PlayState.ComponentManager, "bedbox", this, Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), PrimitiveLibrary.BoxPrimitives["bed"], spriteSheet);

            Voxel voxelUnder = new Voxel();


            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;
        }

    }

    [JsonObject(IsReference = true)]
    public class Bookshelf : Body
    {
        private Box bedModel;
        public Bookshelf()
        {

        }
        // 20 x 8 x 32
        public Bookshelf(Vector3 position) :
            base("Bookshelf", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.5f, 0.5f, 0.5f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bookshelf);
            bedModel = new Box(PlayState.ComponentManager, "model", this, Matrix.CreateTranslation(new Vector3(-20.0f / 64.0f, -32.0f / 64.0f, -8.0f / 64.0f)), new Vector3(32.0f / 32.0f, 8.0f / 32.0f, 20.0f / 32.0f), new Vector3(0.0f, 0.0f, 0.0f), PrimitiveLibrary.BoxPrimitives["bookshelf"], spriteSheet);
           
            Voxel voxelUnder = new Voxel();


            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Books");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }
    }
}
