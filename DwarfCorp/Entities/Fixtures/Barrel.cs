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
    public class Barrel : CraftedBody
    {
        [EntityFactory("Barrel")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Barrel(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        private static RawPrimitive SharedMesh = null;

        public Barrel()
        {

        }

        public Barrel(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Barrel", Matrix.CreateTranslation(position),
                new Vector3(0.9f, 0.9f, 0.9f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new CraftDetails(manager, Resource))
        {
            Name = "Barrel";
            Tags.Add("Barrel");
            CollisionType = CollisionType.Static;

            CreateCosmeticChildren(manager);
            //PropogateTransforms();
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            if (GetComponent<MeshComponent>().HasValue(out var prim))
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
                SharedMesh = AssetManager.GetContentMesh("Entities/Furniture/barrel");

            AddChild(new MeshComponent(Manager,
                Matrix.Identity,
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                SharedMesh,
                "Entities/Furniture/tx_barrel"))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);
        }
    }
}
