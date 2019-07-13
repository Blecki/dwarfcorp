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

        private static GeometricPrimitive SharedPrimitive = null;

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
            if (GetComponent<PrimitiveComponent>().HasValue(out var prim)) // Todo: Won't the prim component handle rendering...???
                prim.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            if (SharedPrimitive == null)
            {
                var spriteSheet = new NamedImageFrame("Entities\\Furniture\\bookshelf");
                SharedPrimitive = new OldBoxPrimitive(DwarfGame.GuiSkin.Device, 0.625f, 1.0f, 0.25f,
                        new OldBoxPrimitive.BoxTextureCoords(spriteSheet.SafeGetImage().Width, spriteSheet.SafeGetImage().Height,
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 20, 20, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(28, 20, 20, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(20, 0, 8, 20), false),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 0, 1, 1), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(20, 20, 8, 32), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(20, 52, 8, 32), true)));
            }

            AddChild(new PrimitiveComponent(Manager,
                Matrix.CreateTranslation(new Vector3(-0.25f, 0.0f, 0.35f - 0.15f)),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.0f, 0.0f, 0.0f),
                SharedPrimitive,
                "Entities\\Furniture\\bookshelf"))
                .SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.5f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false).SetFlag(Flag.RotateBoundingBox, true);

        }
    }
}
