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
    public class Bed : Body, IRenderableComponent
    {
        public Bed()
        {
            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Bed(ComponentManager manager, Vector3 position) :
            base(manager, "Bed", Matrix.CreateTranslation(position), new Vector3(1.5f, 0.5f, 0.75f), new Vector3(-0.5f + 1.5f * 0.5f, -0.5f + 0.25f, -0.5f + 0.75f * 0.5f))
        {
            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData, 
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager,
                    voxelUnder));

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
            
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            var spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);

            AddChild(new Box(Manager, 
                "bedbox", 
                Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), 
                new Vector3(1.0f, 1.0f, 2.0f), 
                new Vector3(0.5f, 0.5f, 1.0f), 
                "bed", 
                spriteSheet)).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
