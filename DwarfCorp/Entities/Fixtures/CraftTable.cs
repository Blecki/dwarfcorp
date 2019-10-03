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
    public class CraftTable : CraftedBody
    {
        [EntityFactory("CraftTable")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CraftTable(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        private static GeometricPrimitive SharedPrimitive = null;
        
        public CraftTable()
        {

        }

        public CraftTable(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Craft Table", Matrix.CreateTranslation(position), 
                new Vector3(0.9f, 0.4f, 0.9f),
                new Vector3(0.0f, 0.25f, 0.0f), 
                new DwarfCorp.CraftDetails(manager, Resource))
        {
            Tags.Add("Craft Table");
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

            if (SharedPrimitive == null)
            {
                var spriteSheet = new NamedImageFrame("Entities\\Furniture\\craft-table");
                SharedPrimitive = new OldBoxPrimitive(DwarfGame.GuiSkin.Device, 0.8f, 0.5f, 0.8f,
                        new OldBoxPrimitive.BoxTextureCoords(spriteSheet.SafeGetImage().Width, spriteSheet.SafeGetImage().Height,
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 32, 32, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 32, 32, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 0, 32, 32), false),
                            new OldBoxPrimitive.FaceData(new Rectangle(16, 47, 1, 1), false),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 32, 32, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 32, 32, 16), true)));
            }

            AddChild(new PrimitiveComponent(Manager,
                Matrix.CreateTranslation(-0.40f, 0.00f, -0.40f) * Matrix.CreateRotationY((float)Math.PI * 0.5f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                SharedPrimitive,
                "Entities\\Furniture\\craft-table"))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);

            AddChild(new GenericVoxelListener(Manager,
                Matrix.Identity,
                new Vector3(0.75f, 0.5f, 0.75f), // Position just below surface.
                new Vector3(0.0f, -0.30f, 0.0f),
                (v) =>
                {
                    if (v.Type == VoxelChangeEventType.VoxelTypeChanged
                        && v.NewVoxelType == 0)
                        Die();
                }))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);
        }
    }
}
