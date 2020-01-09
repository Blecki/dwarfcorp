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
    public class Cart : CraftedBody
    {
        [EntityFactory("Cart")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cart(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        private static RawPrimitive SharedMesh = null;
        
        public Cart()
        {

        }

        public Cart(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Cart", Matrix.CreateTranslation(position), 
                new Vector3(0.9f, 0.9f, 0.9f),
                new Vector3(0.25f, 0.25f, 0.0f), 
                new CraftDetails(manager, Resource))
        {
            Tags.Add("Cart");
            CollisionType = CollisionType.Static;
            SetFlag(Flag.RotateBoundingBox, true);
            
            CreateCosmeticChildren(manager);

            OrientToWalls();
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            if (GetComponent<PrimitiveComponent>().HasValue(out var prim))
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

            if (SharedMesh == null)
                SharedMesh = AssetManager.GetContentMesh("Entities/Rail/sm_minecart");

            AddChild(new MeshComponent(Manager,
                Matrix.Identity,
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                SharedMesh,
                "Entities/Rail/minecart_tx"))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);
        }
    }
}
