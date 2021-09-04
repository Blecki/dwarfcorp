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
    public class Bed : CraftedBody
    {
        [EntityFactory("Bed")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bed(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        private static GeometricPrimitive SharedPrimitive = null;
        
        public Bed()
        {

        }

        public Bed(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, "Bed", Matrix.CreateTranslation(position), 
                new Vector3(1.9f, 0.4f, 0.9f),
                new Vector3(0.5f, 0.25f, 0.0f), 
                new CraftDetails(manager, Resource))
        {
            Tags.Add("Bed");
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
                var spriteSheet = new NamedImageFrame("Entities\\Furniture\\bedtex");
                SharedPrimitive = new OldBoxPrimitive(DwarfGame.GuiSkin.Device, 0.8f, 0.5f, 1.8f,
                        new OldBoxPrimitive.BoxTextureCoords(spriteSheet.SafeGetImage().Width, spriteSheet.SafeGetImage().Height,
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 24, 24, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(72, 24, 24, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(24, 0, 48, 24), false),
                            new OldBoxPrimitive.FaceData(new Rectangle(0, 0, 1, 1), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(24, 24, 48, 16), true),
                            new OldBoxPrimitive.FaceData(new Rectangle(24, 40, 48, 16), true)));
            }

            AddChild(new PrimitiveComponent(Manager,
                Matrix.CreateTranslation(-0.40f, 0.00f, -0.45f) * Matrix.CreateRotationY((float)Math.PI * 0.5f),
                new Vector3(1.0f, 1.0f, 2.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                SharedPrimitive,
                "Entities\\Furniture\\bedtex"))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);

            AddChild(new GenericVoxelListener(Manager,
                Matrix.Identity,
                new Vector3(1.5f, 0.5f, 0.75f), // Position just below surface.
                new Vector3(0.5f, -0.30f, 0.0f),
                (v) =>
                {
                    if (v.Type == VoxelEventType.VoxelTypeChanged
                        && v.NewVoxelType == 0)
                        Die();
                }))
                .SetFlag(Flag.ShouldSerialize, false)
                .SetFlag(Flag.RotateBoundingBox, true);
        }
    }
}
