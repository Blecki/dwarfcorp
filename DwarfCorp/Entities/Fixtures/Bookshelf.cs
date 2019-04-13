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
    public class Bookshelf : CraftedBody
    {
        [EntityFactory("Bookshelf")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bookshelf(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null)) { Tags = new List<string>() { "Research" } };
        }

        public Bookshelf()
        {
            CollisionType = CollisionType.Static;
        }

        public Bookshelf(ComponentManager manager, Vector3 position, List<ResourceAmount> resources) :
            base(manager, "Bookshelf", Matrix.CreateTranslation(position), 
                new Vector3(0.5f, 0.9f, 0.28f), 
                new Vector3(0.0f, 0.5f, 0.35f), new CraftDetails(manager, "Bookshelf", resources))
        {
            Tags.Add("Books");
            CollisionType = CollisionType.Static;
            SetFlag(Flag.RotateBoundingBox, true);

            CreateCosmeticChildren(manager);
            OrientToWalls();
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            GetComponent<Box>().Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            // Only renders to selection buffer

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            AddChild(new Box(Manager,
                "model",
                Matrix.CreateTranslation(new Vector3(-0.25f, 0.0f, 0.35f - 0.15f)),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.0f, 0.0f, 0.0f),
                "bookshelf",
                ContentPaths.Entities.Furniture.bookshelf)).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.5f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false).SetFlag(Flag.RotateBoundingBox, true);

        }
    }
}
